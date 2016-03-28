using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Kml2Sql.MsSql
{
    public class CommandCreator
    {
        public Kml2SqlConfig Configuration { get; private set; } = new Kml2SqlConfig();
        private IEnumerable<MapFeature> _mapFeatures;

        public CommandCreator(Stream fileStream, Kml2SqlConfig configuration) : this(fileStream)
        {
            if (configuration != null)
            {
                Configuration = configuration;
            }           
        }

        public CommandCreator(Stream fileStream)
        {
            var kml = KMLParser.Parse(fileStream);
            _mapFeatures = GetPlacemarks(kml);
        }

        private IEnumerable<MapFeature> GetPlacemarks(Kml kml)
        {
            int id = 1;
            foreach (var placemark in kml.Flatten().OfType<Placemark>())
            {

                if (HasValidElement(placemark))
                {
                    MapFeature mapFeature = new MapFeature(placemark, id);
                    yield return mapFeature;
                }
                id++;
            }
        }

        public IEnumerable<SqlCommand> GetInsertCommands()
        {
            foreach (var mf in _mapFeatures)
            {
                var command = CreateCommand(mf);
                yield return command;
            }
        }

        public IEnumerable<SqlCommand> GetInsertCommands(SqlConnection connection)
        {
            foreach (var command in GetInsertCommands())
            {
                command.Connection = connection;
                yield return command;
            }
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


        public string GetCreateTableScript()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("CREATE TABLE [{0}] (", Configuration.TableName));
            sb.Append($"[{Configuration.IdColumnName}] INT NOT NULL PRIMARY KEY,");
            foreach (var columnName in GetColumnNames().Select(Configuration.GetColumnName))
            {
                sb.Append(String.Format("[{0}] VARCHAR(max), ", columnName));
            }
            sb.Append(String.Format("[{0}] [sys].[{1}] NOT NULL, );", Configuration.PlacemarkColumnName, Configuration.GeoType));
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

        private SqlCommand CreateCommand(MapFeature mapFeature)
        {
            StringBuilder sbColumns = new StringBuilder();
            StringBuilder sbValues = new StringBuilder();
            foreach (KeyValuePair<string, string> simpleData in mapFeature.Data)
            {
                sbColumns.Append(Configuration.GetColumnName(simpleData.Key) + ",");
                sbValues.Append("@" + Configuration.GetColumnName(simpleData.Key) + ",");
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(ParseCoordinates(mapFeature));
            sb.Append(string.Format("INSERT INTO {0}(Id,{1}{2}) VALUES(@Id,{3}@placemark)", Configuration.TableName, sbColumns, Configuration.PlacemarkColumnName, sbValues));
            string sqlCommandText = sb.ToString();
            SqlCommand sqlCommand = new SqlCommand(sqlCommandText);
            sqlCommand.Parameters.AddWithValue("@Id", mapFeature.Id);
            foreach (KeyValuePair<string, string> simpleData in mapFeature.Data)
            {
                sqlCommand.Parameters.AddWithValue("@" + Configuration.GetColumnName(simpleData.Key), simpleData.Value);
            }
            return sqlCommand;
        }

        private string ParseCoordinates(MapFeature mapFeature)
        {
            StringBuilder commandString = new StringBuilder();
            if (Configuration.GeoType == GeoType.Geography)
            {
                commandString.Append(ParseCoordinatesGeography(mapFeature));
                commandString.Append("DECLARE @placemark geography;");
                commandString.Append("SET @placemark = @validGeo;");
            }
            else
            {
                commandString.Append(ParseCoordinatesGeometry(mapFeature));
                commandString.Append("DECLARE @placemark geometry;");
                commandString.Append("SET @placemark = @validGeom;");
            }
            return commandString.ToString();
        }

        private string ParseCoordinatesGeometry(MapFeature mapFeature)
        {
            switch (mapFeature.ShapeType)
            {
                case ShapeType.Polygon: return CreatePolygon(mapFeature);
                case ShapeType.LineString: return CreateLineString(mapFeature);
                case ShapeType.Point: return CreatePoint(mapFeature);
                default: throw new Exception("Unsupported shape type!");
            }
        }

        private string CreatePoint(MapFeature mapFeature)
        {
            var sb = new StringBuilder();
            sb.Append(@"DECLARE @validGeom geometry;" + Environment.NewLine);
            sb.Append("SET @validGeom = geometry::STPointFromText('POINT (");
            sb.Append(mapFeature.Coordinates[0].Longitude + " " + mapFeature.Coordinates[0].Latitude);
            sb.Append(@")', " + Configuration.Srid + @");" + Environment.NewLine);
            return sb.ToString();
        }

        private string CreateLineString(MapFeature mapFeature)
        {
            var sb = new StringBuilder();
            sb.Append("DECLARE @validGeom geometry;" + Environment.NewLine);
            sb.Append("SET @validGeom = geometry::STLineFromText('LINESTRING (");
            foreach (Vector coordinate in mapFeature.Coordinates)
            {
                sb.Append(coordinate.Longitude + " " + coordinate.Latitude + ", ");
            }
            sb.Remove(sb.Length - 2, 2).ToString();
            sb.Append(@")', " + Configuration.Srid + @");");
            return sb.ToString();
        }

        private string CreatePolygon(MapFeature mapFeature)
        {
            var sb = new StringBuilder();
            sb.Append("DECLARE @geom geometry;" + Environment.NewLine);
            sb.Append("SET @geom = geometry::STPolyFromText('POLYGON((");
            sb.Append(GetOuterRingSql(mapFeature.Coordinates));
            foreach (Vector[] innerCoordinates in mapFeature.InnerCoordinates)
            {
                sb.Append(GetInnerRingSql(innerCoordinates));
            }
            sb.Append(@"))', " + Configuration.Srid + @");" + Environment.NewLine);
            sb.Append("DECLARE @validGeom geometry;" + Environment.NewLine);
            sb.Append("SET @validGeom = @geom.MakeValid().STUnion(@geom.STStartPoint());");
            return sb.ToString();
        }

        private string GetOuterRingSql(Vector[] coordinates)
        {
            var outerCoordSql = coordinates.Select(GetVectorSql).ToList();
            if (Configuration.FixPolygons && RingInvalid(coordinates))
            {
                outerCoordSql.Add(GetVectorSql(coordinates[0]));
            }
            var joined = string.Join(", ", outerCoordSql);
            return joined;
        }

        private static string GetInnerRingSql(Vector[] innerCoordinates)
        {
            var sb = new StringBuilder();
            sb.Append("), (");
            var coordSql = innerCoordinates.Select(GetVectorSql);
            sb.Append(string.Join(", ", coordSql));
            return sb.ToString();
        }

        public static string GetVectorSql(Vector coordinate)
        {
            return $"{coordinate.Longitude} {coordinate.Latitude}";
        }

        private static bool RingInvalid(Vector[] coordinates)
        {
            return coordinates.First().Latitude != coordinates.Last().Latitude ||
                coordinates.First().Longitude != coordinates.Last().Longitude;
        }

        private string ParseCoordinatesGeography(MapFeature mapFeature)
        {
            StringBuilder commandString = new StringBuilder();
            commandString.Append(ParseCoordinatesGeometry(mapFeature));
            commandString.Append("DECLARE @validGeo geography;");
            commandString.Append("SET @validGeo = geography::STGeomFromText(@validGeom.STAsText(), " + Configuration.Srid + @").MakeValid();");
            return commandString.ToString();
        }
    }
}
