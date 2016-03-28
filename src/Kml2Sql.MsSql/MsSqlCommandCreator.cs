using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;

namespace Kml2Sql.MsSql
{
    static class MsSqlCommandCreator
    {
        public static SqlCommand CreateCommand(MapFeature mapFeature, GeoType geographyMode, int srid, string tableName, 
            string placemarkColumnName, SqlConnection connection, bool forceValid)
        {
            StringBuilder sbColumns = new StringBuilder();
            StringBuilder sbValues = new StringBuilder();
            foreach (KeyValuePair<string, string> simpleData in mapFeature.Data)
            {
                sbColumns.Append(simpleData.Key + ",");
                sbValues.Append("@" + simpleData.Key + ",");
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(ParseCoordinates(srid, mapFeature, geographyMode, forceValid));
            sb.Append(string.Format("INSERT INTO {0}(Id,{1}{2}) VALUES(@Id,{3}@placemark)", tableName, sbColumns, placemarkColumnName, sbValues));
            string sqlCommandText = sb.ToString();
            SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
            sqlCommand.Parameters.AddWithValue("@Id", mapFeature.Id);
            foreach (KeyValuePair<string, string> simpleData in mapFeature.Data)
            {
                sqlCommand.Parameters.AddWithValue("@" + simpleData.Key, simpleData.Value);
            }
            return sqlCommand;
        }

        private static string ParseCoordinates(int srid, MapFeature mapFeature, GeoType geographyMode, bool forceValid)
        {
            StringBuilder commandString = new StringBuilder();
            if (geographyMode == GeoType.Geography)
            {
                commandString.Append(ParseCoordinatesGeography(srid, mapFeature, forceValid));
                commandString.Append("DECLARE @placemark geography;");
                commandString.Append("SET @placemark = @validGeo;");
            }
            else
            {
                commandString.Append(ParseCoordinatesGeometry(srid, mapFeature, geographyMode, forceValid));
                commandString.Append("DECLARE @placemark geometry;");
                commandString.Append("SET @placemark = @validGeom;");
            }
            return commandString.ToString();
        }

        private static string ParseCoordinatesGeometry(int srid, MapFeature mapFeature, GeoType geographyMode, bool forceClose)
        {
            StringBuilder commandString = new StringBuilder();
            switch (mapFeature.ShapeType)
            {
                case ShapeType.Polygon:
                    {
                        commandString.Append(@"DECLARE @geom geometry;
                                        SET @geom = geometry::STPolyFromText('POLYGON((");
                        foreach (Vector coordinate in mapFeature.Coordinates)
                        {
                            commandString.Append(coordinate.Longitude + " " + coordinate.Latitude + ", ");
                        }
                        if (forceClose && RingInvalid(mapFeature.Coordinates))
                        {
                            commandString.Append(mapFeature.Coordinates[0].Longitude + " " + mapFeature.Coordinates[0].Latitude + ", ");
                        }
                        commandString.Remove(commandString.Length - 2, 2).ToString();

                        foreach (Vector[] innerCoordinates in mapFeature.InnerCoordinates)
                        {
                            commandString.Append("), (");
                            foreach (Vector coordinate in innerCoordinates)
                            {
                                commandString.Append(coordinate.Longitude + " " + coordinate.Latitude + ", ");
                            }
                            commandString.Remove(commandString.Length - 2, 2).ToString();
                        }

                        commandString.Append(@"))', " + srid + @");");
                        commandString.Append("DECLARE @validGeom geometry;");
                        commandString.Append("SET @validGeom = @geom.MakeValid().STUnion(@geom.STStartPoint());");
                    }
                    break;
                case ShapeType.LineString:
                    {
                        commandString.Append(@"DECLARE @validGeom geometry;
                                    SET @validGeom = geometry::STLineFromText('LINESTRING (");
                        foreach (Vector coordinate in mapFeature.Coordinates)
                        {
                            commandString.Append(coordinate.Longitude + " " + coordinate.Latitude + ", ");
                        }
                        commandString.Remove(commandString.Length - 2, 2).ToString();
                        commandString.Append(@")', " + srid + @");");
                    }
                    break;
                case ShapeType.Point:
                    {
                        commandString.Append(@"DECLARE @validGeom geometry;");
                        commandString.Append("SET @validGeom = geometry::STPointFromText('POINT (");
                        commandString.Append(mapFeature.Coordinates[0].Longitude + " " + mapFeature.Coordinates[0].Latitude);
                        commandString.Append(@")', " + srid + @");");
                    }
                    break;
                default:
                {
                    //Do nothing. It's probably polygon point we don't support.
                }
                    break;
            }
            return commandString.ToString();
        }

        private static bool RingInvalid(Vector[] coordinates)
        {
            return coordinates.First().Latitude != coordinates.Last().Latitude ||
                coordinates.First().Longitude != coordinates.Last().Longitude;
        }

        private static string ParseCoordinatesGeography(int srid, MapFeature mapFeature, bool forceValid)
        {
            StringBuilder commandString = new StringBuilder();
            commandString.Append(ParseCoordinatesGeometry(srid, mapFeature, GeoType.Geography, forceValid));
            commandString.Append("DECLARE @validGeo geography;");
            commandString.Append("SET @validGeo = geography::STGeomFromText(@validGeom.STAsText(), " + srid + @").MakeValid();");
            return commandString.ToString();
        }
    }
}
