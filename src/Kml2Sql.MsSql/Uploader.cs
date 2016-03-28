using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Kml2Sql.MsSql
{
    public static class Uploader
    {
        public static void Upload(FileStream stream, string connectionString, Kml2SqlConfig config = null)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                Upload(stream, connection, config);
            }
        }

        public static void Upload(string filePath, string connectionString, Kml2SqlConfig config = null)
        {
            using (var stream = File.OpenRead(filePath))
            {
                Upload(stream, connectionString, config);
            }
        }

        public static void Upload(string filePath, SqlConnection connection, Kml2SqlConfig config = null)
        {
            using (var stream = File.OpenRead(filePath))
            {
                Upload(stream, connection, config);
            }
        }

        public static void Upload(FileStream stream, SqlConnection connection, Kml2SqlConfig config = null)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            var commandCreator = new CommandCreator(stream, config);
            var tableCommand = commandCreator.GetCreateTableCommand(connection);
            tableCommand.ExecuteNonQuery();
            var insertCommands = commandCreator.GetInsertCommands(connection);
            foreach (var c in insertCommands)
            {
                c.ExecuteNonQuery();
            }
        }


        public static void DropTable(SqlConnection connection, string tableName)
        {
            string dropCommandString = String.Format("DROP TABLE {0};", tableName);
            var dropCommand = new SqlCommand(dropCommandString, connection);
            dropCommand.CommandType = System.Data.CommandType.Text;
            dropCommand.ExecuteNonQuery();
        }
    }
}
