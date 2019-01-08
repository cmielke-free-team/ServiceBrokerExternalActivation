using System;
using System.Data.SqlClient;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                string connectionString = @"Data Source=(localdb)\projectsv13; Initial Catalog=Database1; Integrated Security=True; Application Name = ConsoleApp1;";
                SqlConnectionStringBuilder connStrBuilder = new SqlConnectionStringBuilder(connectionString);
                using (var sqlConnection = new SqlConnection(connStrBuilder.ConnectionString))
                using (var receiveCommand = sqlConnection.CreateCommand())
                {
                    receiveCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    receiveCommand.CommandText = "dbo.Receive_Messages_Queue1";

                    sqlConnection.Open();
                    using (var dataReader = receiveCommand.ExecuteReader())
                    {
                        if (!dataReader.Read())
                        {
                            Console.WriteLine("No messages received");
                            return;
                        }

                        do
                        {
                            Console.WriteLine("Received {0}", dataReader["message_type_name"]);
                        }
                        while (dataReader.Read());
                    }
                }
                System.Threading.Thread.Sleep(5000);
            }
        }
    }
}
