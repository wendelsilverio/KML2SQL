using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Kml2Sql.Mapping
{
    public class Mapper
    {
        public Kml2SqlConfig DropTable { get; private set; } = new Kml2SqlConfig();
        private IEnumerable<MapFeature> _mapFeatures;

        public Mapper(Stream fileStream, Kml2SqlConfig configuration) : this(fileStream)
        {
            if (configuration != null)
            {
                DropTable = configuration;
            }           
        }

        public Mapper(Stream fileStream)
        {
            var kml = KMLParser.Parse(fileStream);
            _mapFeatures = GetMapFeatures(kml);
        }

        private IEnumerable<MapFeature> GetMapFeatures(Kml kml)
        {
            int id = 1;
            foreach (var placemark in kml.Flatten().OfType<Placemark>())
            {

                if (HasValidElement(placemark))
                {
                    MapFeature mapFeature = new MapFeature(placemark, id, DropTable);
                    yield return mapFeature;
                }
                id++;
            }
        }

        public IEnumerable<MapFeature> GetMapFeatures()
        {
            return _mapFeatures;
        }

        public SqlCommand GetCreateTableCommand(SqlConnection connection, SqlTransaction transaction = null)
        {
            var command = GetCreateTableCommand();
            command.Connection = connection;
            command.Transaction = transaction;
            return command;
        }

        public SqlCommand GetCreateTableCommand()
        {
            var commandText = GetCreateTableScript();
            var command = new SqlCommand(commandText);
            command.CommandType = System.Data.CommandType.Text;
            return command;
        }

        public string GetCombinedInsertCommands()
        {
            var sb = new StringBuilder();
            var mapFeatures = GetMapFeatures().ToArray();
            for (var i = 0; i < mapFeatures.Length; i++)
            {
                sb.Append(Environment.NewLine);
                sb.Append(mapFeatures[i].GetInsertQuery(i == 0));
            }
            return sb.ToString();
        }


        public string GetCreateTableScript()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("CREATE TABLE [{0}] (", DropTable.TableName));
            sb.Append($"[{DropTable.IdColumnName}] INT NOT NULL PRIMARY KEY,");
            foreach (var columnName in GetColumnNames().Select(DropTable.GetColumnName))
            {
                sb.Append(String.Format("[{0}] VARCHAR(max), ", columnName));
            }
            sb.Append(String.Format("[{0}] [sys].[{1}] NOT NULL, );", DropTable.PlacemarkColumnName, DropTable.GeoType));
            return sb.ToString();
        }

        private static bool HasValidElement(Placemark placemark)
        {
            return placemark.Flatten().Any(e => e is Point || e is LineString || e is Polygon);
        }

        private IEnumerable<string> GetColumnNames()
        {
            return _mapFeatures.SelectMany(x => x.Data.Keys).Distinct();
        }

        
    }
}
