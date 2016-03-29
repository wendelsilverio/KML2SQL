using Kml2Sql.MsSql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KML2SQL
{
    public class Uploader
    {
        public IProgress<int> OnProgressChange { get; set; }

        public Mapper Mapper { get; private set; }

        public Uploader(FileStream stream, Kml2SqlConfig configuration)
        {
            Mapper = new Mapper(stream, configuration);
        }

        public Uploader(string filePath, Kml2SqlConfig configuration)
        {
            using (var stream = File.OpenRead(filePath))
            {
                Mapper = new Mapper(stream, configuration);
            }                
        }

        public Uploader(FileStream stream) : this(stream, null) { }

        public Uploader(string filePath) : this(filePath, null) { }

        public Uploader(FileStream stream, Kml2SqlConfig configuration, Progress<int> onChange) 
            : this(stream, configuration)
        {
            OnProgressChange = onChange;
        }

        public Uploader(string fileStream, Kml2SqlConfig configuration, Progress<int> onChange)
            : this(fileStream, configuration)
        {
            OnProgressChange = onChange;
        }

        public void Upload(string connectionString)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                Upload(conn);
            }
        }

        public void Upload(SqlConnection connection)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            TryDropTable(connection);
            CreateTable(connection);
            var mapFeatures = Mapper.GetMapFeatures().ToArray();
            for (var i = 0; i < mapFeatures.Length; i++)
            {
                var sqlCommand = mapFeatures[i].GetSqlCommand();
                sqlCommand.Connection = connection;
                sqlCommand.ExecuteNonQuery();
                if (OnProgressChange != null)
                {
                    OnProgressChange.Report(GetPercentage(i + 1, mapFeatures.Length));
                }
            }
        }

        public string GetScript()
        {
            var sb = new StringBuilder();
            sb.Append(Mapper.GetCreateTableScript());
            sb.Append(Mapper.GetCombinedInsertCommands());
            if (OnProgressChange != null)
            {
                OnProgressChange.Report(100);
            }
            return sb.ToString();
        }

        private static int GetPercentage(int current, int total)
        {
            if (current == total)
            {
                return 100;
            }
            var percentage = ((double)current / total) * 100;
            return (int)percentage;
        }

        public void CreateTable(SqlConnection connection)
        {
            var tableCommand = Mapper.GetCreateTableCommand(connection);
            tableCommand.ExecuteNonQuery();
        }


        public bool TryDropTable(SqlConnection connection)
        {
            try
            {
                string dropCommandString = String.Format("DROP TABLE {0};", Mapper.Configuration.TableName);
                var dropCommand = new SqlCommand(dropCommandString, connection);
                dropCommand.CommandType = System.Data.CommandType.Text;
                dropCommand.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }            
        }
    }
}
