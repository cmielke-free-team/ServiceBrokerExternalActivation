using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace EmdatSSBEAService
{
    public class NotificationService
    {
        private readonly string _connectionString;
        private readonly string _storedProcedure;
        private readonly ApplicationServiceList _applicationServices;
        private static readonly TraceSource traceSource = new TraceSource("Emdat.SSBEA.Service");

        public NotificationService(NotificationServiceConfig config, ApplicationServiceList applicationServices)
        {
            _connectionString = config.ConnectionString;
            _storedProcedure = config.StoredProcedure;
            _applicationServices = applicationServices;
        }

        internal void Execute(CancellationToken cancellationToken)
        {
            Logger.TraceEvent(TraceEventType.Information, "Notification service execute started.");
            try
            {
                while (!cancellationToken.WaitHandle.WaitOne(500))
                {
                    try
                    {
                        string connectionString = _connectionString;
                        SqlConnectionStringBuilder connStrBuilder = new SqlConnectionStringBuilder(connectionString);
                        using (var sqlConnection = new SqlConnection(connStrBuilder.ConnectionString))
                        using (var cmd = sqlConnection.CreateCommand())
                        {
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.CommandText = _storedProcedure;

                            sqlConnection.Open();
                            using (var dataReader = cmd.ExecuteReader())
                            {
                                while (dataReader.Read())
                                {
                                    try
                                    {
                                        string messageType = (string)dataReader["message_type_name"];
                                        Guid conversationHandle = (Guid)dataReader["conversation_handle"];
                                        byte[] messageBody = dataReader["message_body"] == DBNull.Value ? new byte[0] : (byte[])dataReader["message_body"];
                                        Logger.TraceEvent(TraceEventType.Information, $"Received message type {messageType} on conversation handle {conversationHandle}.");
                                        switch (messageType)
                                        {
                                            case "http://schemas.microsoft.com/SQL/Notifications/EventNotification":
                                            {
                                                HandleEventNotification(messageBody, cancellationToken);
                                                break;
                                            }
                                            case "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog":
                                            {
                                                int end = this.EndConversation(cmd, conversationHandle, null, null);
                                                break;
                                            }
                                            case "http://schemas.microsoft.com/SQL/ServiceBroker/Error":
                                            {
                                                Logger.TraceEvent(TraceEventType.Error, $"Received service broker error with body: {Encoding.Unicode.GetString(messageBody)}");
                                                int end = this.EndConversation(cmd, conversationHandle, null, null);
                                                break;
                                            }
                                        }
                                    }
                                    catch (QueueActivationException ex)
                                    {
                                        Logger.TraceEvent(TraceEventType.Error, $"{ex}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.TraceEvent(TraceEventType.Error, $"{ex}");
                        cancellationToken.WaitHandle.WaitOne(60000);
                    }
                }
            }
            finally
            {
                Logger.TraceEvent(TraceEventType.Information, "Notification service execute stopped.");
            }
        }

        internal void MonitorQueueReaders(CancellationToken cancellationToken)
        {
            Logger.TraceEvent(TraceEventType.Information, "Notification service queue reader monitor started.");
            try
            {
                while (!cancellationToken.WaitHandle.WaitOne(61000))
                {
                    try
                    {
                        _applicationServices.MonitorQueueReaders();
                    }
                    catch (Exception ex)
                    {
                        Logger.TraceEvent(TraceEventType.Error, $"{ex}");
                        cancellationToken.WaitHandle.WaitOne(60000);
                    }
                }
            }
            finally
            {
                Logger.TraceEvent(TraceEventType.Information, "Notification service queue reader monitor stopped.");
            }
        }

        private int EndConversation(SqlCommand cmd, Guid conversationHandle, Int32? failureCode, String failureDescription)
        {
            traceSource.TraceEvent(System.Diagnostics.TraceEventType.Start, 0, "SSBEndConversation");
            try
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = "DATA_Broker.dbo.SSB_End_Conversation";
                cmd.Parameters.Clear();


                SqlParameter conversationHandleParameter = new SqlParameter("@Conversation_Handle", (object)conversationHandle ?? DBNull.Value);
                conversationHandleParameter.Size = 16;
                conversationHandleParameter.Direction = ParameterDirection.Input;
                conversationHandleParameter.SqlDbType = SqlDbType.UniqueIdentifier;
                cmd.Parameters.Add(conversationHandleParameter);


                SqlParameter failureCodeParameter = new SqlParameter("@Failure_Code", (object)failureCode ?? DBNull.Value);
                failureCodeParameter.Size = 4;
                failureCodeParameter.Precision = 10;
                failureCodeParameter.Direction = ParameterDirection.Input;
                failureCodeParameter.SqlDbType = SqlDbType.Int;
                cmd.Parameters.Add(failureCodeParameter);


                SqlParameter failureDescriptionParameter = new SqlParameter("@Failure_Description", (object)failureDescription ?? DBNull.Value);
                failureDescriptionParameter.Size = 3000;
                failureDescriptionParameter.Direction = ParameterDirection.Input;
                failureDescriptionParameter.SqlDbType = SqlDbType.NVarChar;
                cmd.Parameters.Add(failureDescriptionParameter);


                var executeNonQueryResult = cmd.ExecuteNonQuery();
                return executeNonQueryResult;

            }
            finally
            {
                traceSource.TraceEvent(System.Diagnostics.TraceEventType.Stop, 0);
            }
        }

        private void HandleEventNotification(byte[] messageBody, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream(messageBody))
            {
                if (ms.Length == 0)
                {
                    Logger.TraceEvent(TraceEventType.Error, $"Received an empty message body for an event notification.");
                    return;
                }

                XElement xelement = XElement.Load(ms);
                string eventType = (string)xelement.Element("EventType");
                if (eventType != "QUEUE_ACTIVATION")
                {
                    Logger.TraceEvent(TraceEventType.Error, $"Received unexpected event type value:{eventType}");
                    return;
                }
                string databaseName = (string)xelement.Element("DatabaseName");
                string schemaName = (string)xelement.Element("SchemaName");
                string objectName = (string)xelement.Element("ObjectName");
                Logger.TraceEvent(TraceEventType.Information, $"Received event notification for queue activation:{databaseName}.{schemaName}.{objectName}.");

                //Some queue readers need time to "wind down" after the queue is empty (e.g. flush telemetry to App Insights). If 
                //all queue readers happen to be winding down at the same time and a new activation event is received, then the queue 
                //may get stuck in a NOTFIED state. To work around this issue, retry the activation for up to 10 seconds to give the 
                //queue readers time to finish winding down.
                DateTime retryUntil = DateTime.Now.AddSeconds(10);
                bool activated = false;
                do
                {
                    try
                    {
                        _applicationServices.Activate(databaseName, schemaName, objectName);
                        activated = true;
                    }
                    catch(MaximumQueueReadersException ex)
                    {
                        Logger.TraceEvent(TraceEventType.Information, ex.Message);
                    }
                }
                while (!activated && DateTime.Now < retryUntil && !cancellationToken.WaitHandle.WaitOne(100));
            }
        }
    }
}