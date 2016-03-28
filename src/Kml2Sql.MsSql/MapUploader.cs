using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using SharpKml.Dom;
using SharpKml.Engine;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Kml2Sql.MsSql
{
    public class MapUploader : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        string _placemarkColumnName, _tableName, _sqlGeoType;
        readonly StringBuilder _log = new StringBuilder();
        GeoType geoType;
        int _srid;
        BackgroundWorker _worker;
        readonly List<MapFeature> _mapFeatures = new List<MapFeature>();
        readonly List<string> _columnNames = new List<string>();
        private string _progress = "";
        bool _forceValid;
        public Action<string> UhandledExceptionWriter { get; set; }
        public string LogFolder { get; set; }

        public string Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                this.OnPropertyChanged("Progress");
                _log.Append(value + Environment.NewLine);
            }
        }

        public MapUploader(string connectionString)
        {
            _connectionString = connectionString;
            LogFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\KML2SQL";
        }

        private void InitializeMapFeatures(Kml kml)
        {
            foreach (var mapFeature in GetPlacemarks(kml))
            {
                _mapFeatures.Add(mapFeature);
                foreach (KeyValuePair<string, string> pair in mapFeature.Data)
                {
                    if (!_columnNames.Contains(pair.Key) && pair.Key.ToLower() != "id")
                    {
                        _columnNames.Add(pair.Key);
                    }
                }
            }
        }

        private void InitializeBackgroundWorker()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += bw_DoWork;
            _worker.ProgressChanged += bw_ProgressChanged;
            _worker.WorkerSupportsCancellation = true;
        }

        public void Upload(string columnName, string fileLocation, string tableName, int srid, GeoType geoType, bool forceValid = false)
        {
            _placemarkColumnName = columnName;
            _tableName = tableName;
            this.geoType = geoType;
            _srid = srid;
            _sqlGeoType = geoType == GeoType.Geography ? "geography" : "geometry";
            _forceValid = forceValid;
            Kml kml = KMLParser.Parse(fileLocation);
            InitializeMapFeatures(kml);
            InitializeBackgroundWorker();
#if !DEBUG
            _worker.RunWorkerAsync();
#else
            DoWork();
#endif
        }

        private void DoWork()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    DropTable(connection);
                    CreateTable(connection);
                    foreach (MapFeature mapFeature in _mapFeatures)
                    {
                        SqlCommand command;
                        try
                        {
                            command = MsSqlCommandCreator.CreateCommand(mapFeature, geoType, _srid,
                                _tableName, _placemarkColumnName, connection, _forceValid);
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            _worker.ReportProgress(0, ex.Message);
                        }
                        _worker.ReportProgress(0, String.Format("Uploading Placemark # {0}", mapFeature.Id));
                    }
                    _worker.ReportProgress(0, "Done!");
                }
            }
            catch (Exception ex)
            {
                _worker.ReportProgress(0, ex.Message + "\r\n\r\n" + ex);
                _worker.CancelAsync();
                if (this.UhandledExceptionWriter != null)
                {
                    var errorText = "The process failed with the following error: \r\n\r\n " + ex.Message;
                    UhandledExceptionWriter(errorText);
                }
                //MessageBox.Show("The process failed with the following error: \r\n\r\n " + ex.Message, "Error", 
                //    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                WriteOutLog();
            }
        }

        private void WriteOutLog()
        {
            string logFile = String.Format("{0}\\KML2SQL_Log_{1:yyyy-MM-dd-hhmmss-fff}.txt", LogFolder, DateTime.Now);
            using (var writer = new StreamWriter(logFile, true))
            {
                if (_log != null)
                    writer.Write(_log);
            }
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            DoWork();
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress = e.UserState.ToString();
        }

        private IEnumerable<MapFeature> GetPlacemarks(Kml kml)
        {
            int id = 1;
            foreach (var placemark in kml.Flatten().OfType<Placemark>())
            {

                if (placemark.Flatten().Any(p => IsValidType(p)))
                {
                    MapFeature mapFeature = new MapFeature(placemark, id);
                    yield return mapFeature;
                }
                else
                {
                    _log.Append("The map feature '" + placemark.Name + "' was not a Polygon, Linestring, " +
                                "or Point type. It will be skipped.\r\n");
                }                
                id++;
            }
        }

        private static bool IsValidType(Element e)
        {
            return e is Point || e is LineString || e is Polygon;
        }

        private void DropTable(SqlConnection connection)
        {
            try
            {
                string dropCommandString = String.Format("DROP TABLE {0};", _tableName);
                var dropCommand = new SqlCommand(dropCommandString, connection);
                dropCommand.CommandType = System.Data.CommandType.Text;
                dropCommand.ExecuteNonQuery();
                _worker.ReportProgress(0, "Existing Table Dropped");
            }
            catch
            {
                _worker.ReportProgress(0, "Could not drop table. This is most likely becuase there was no table to drop, " +
                                          "but it may be because you do not have sufficient priviledges.");
            }
        }

        private void CreateTable(SqlConnection connection)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format("CREATE TABLE [{0}] (", _tableName));
                sb.Append("[Id] INT NOT NULL PRIMARY KEY,");
                if (_columnNames.Count > 0)
                {
                    foreach (string columnName in _columnNames)
                        sb.Append(String.Format("[{0}] VARCHAR(max), ", columnName));
                }
                sb.Append(String.Format("[{0}] [sys].[{1}] NOT NULL, );", _placemarkColumnName, _sqlGeoType));
                var command = new SqlCommand(sb.ToString(), connection);
                command.CommandType = System.Data.CommandType.Text;
                command.ExecuteNonQuery();
                _worker.ReportProgress(0, "Table Created");
            }
            catch (Exception ex)
            {
                _worker.ReportProgress(0, "Could not create table. You may not have sufficient priviledges. Full error log is: " + ex.Message);
                throw;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion
    }
}
