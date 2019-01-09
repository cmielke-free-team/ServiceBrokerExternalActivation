using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            while (!cancellationToken.WaitHandle.WaitOne(500))
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
                                switch (messageType)
                                {
                                    case "http://schemas.microsoft.com/SQL/Notifications/EventNotification":
                                    {
                                        HandleEventNotification((byte[])dataReader["message_body"]);
                                        break;
                                    }
                                    case "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog":
                                    {
                                        int end = this.EndConversation(cmd, (Guid)dataReader["conversation_handle"], null, null);
                                        break;
                                    }
                                    case "http://schemas.microsoft.com/SQL/ServiceBroker/Error":
                                    {
                                        int end = this.EndConversation(cmd, (Guid)dataReader["conversation_handle"], null, null);
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

        private void HandleEventNotification(byte[] messageBody)
        {
            using (var ms = new MemoryStream(messageBody))
            {
                XElement xelement = XElement.Load(ms);
                string eventType = (string)xelement.Element("EventType");
                if (eventType != "QUEUE_ACTIVATION")
                {
                    throw new NotImplementedException("TODO");
                }

                string databaseName = (string)xelement.Element("DatabaseName");
                string schemaName = (string)xelement.Element("SchemaName");
                string objectName = (string)xelement.Element("ObjectName");
                _applicationServices.Activate(databaseName, schemaName, objectName);
            }
        }
    }
}