using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Data.SqlClient;

namespace Kml2Sql.MsSql
{
    public class MapFeature
    {
        public Placemark Placemark { get; private set; }

        public int Id;
        public string Name {get { return Placemark.Name ?? Id.ToString(); }}
        public Vector[] Coordinates { get; private set; }
        public Vector[][] InnerCoordinates { get; private set; }
        public Dictionary<string, string> Data = new Dictionary<string, string>();
        private Kml2SqlConfig _configuration;

        public ShapeType ShapeType { get; private set; }

        internal MapFeature(Placemark placemark, int id, Kml2SqlConfig config)
        {
            Placemark = placemark;
            Id = id;
            SetGeoTypes(placemark);
            InitializeCoordinates(placemark);
            InitializeData(placemark);
            _configuration = config;
        }

        private void SetGeoTypes(Placemark placemark)
        {
            foreach (var element in placemark.Flatten())
            {
                if (element is Point)
                {
                    ShapeType = ShapeType.Point;
                }
                else if (element is Polygon)
                {
                    ShapeType = ShapeType.Polygon;
                }
                else if (element is LineString)
                {
                    ShapeType = ShapeType.LineString;
                }
            }
        }


        private void InitializeCoordinates(Placemark placemark)
        {
            switch (this.ShapeType)
            {
                case ShapeType.LineString:
                    Coordinates = InitializeLineCoordinates(placemark);
                    break;
                case ShapeType.Point:
                    Coordinates = InitializePointCoordinates(placemark);
                    break;
                case ShapeType.Polygon:
                    Vector[][] coords = InitializePolygonCoordinates(placemark);
                    Coordinates = coords[0];
                    if (coords.Length > 1)
                    {
                        InnerCoordinates = coords.Skip(1).ToArray();
                    }
                    else
                    {
                        InnerCoordinates = new Vector[0][];
                    }
                    break;
            }
        }

        private void InitializeData(Placemark placemark)
        {
            foreach (SimpleData sd in placemark.Flatten().OfType<SimpleData>())
            {
                if (sd.Name.ToLower() == "id")
                {
                    sd.Name = "sd_id";
                }
                Data.Add(sd.Name.Sanitize(), sd.Text.Sanitize());
            }
            foreach (Data data in placemark.Flatten().OfType<Data>())
            {
                if (data.Name.ToLower() == "id")
                {
                    data.Name = "data_id";
                }
                Data.Add(data.Name.Sanitize(), data.Value.Sanitize());
            }
        }

        private static Vector[] InitializePointCoordinates(Placemark placemark)
        {
            List<Vector> coordinates = new List<Vector>();
            foreach (var point in placemark.Flatten().OfType<Point>())
            {
                Vector myVector = new Vector();
                myVector.Latitude = point.Coordinate.Latitude;
                myVector.Longitude = point.Coordinate.Longitude;
                coordinates.Add(myVector);
            }
            return coordinates.ToArray();
        }

        private static Vector[] InitializeLineCoordinates(Placemark placemark)
        {
            List<Vector> coordinates = new List<Vector>();
            foreach (LineString element in placemark.Flatten().OfType<LineString>())
            {
                LineString lineString = element;
                coordinates.AddRange(lineString.Coordinates);
            }
            return coordinates.ToArray();
        }

        private static Vector[][] InitializePolygonCoordinates(Placemark placemark)
        {
            List<List<Vector>> coordinates = new List<List<Vector>>();
            coordinates.Add(new List<Vector>());

            foreach (var polygon in placemark.Flatten().OfType<Polygon>())
            {
                coordinates[0].AddRange(polygon.OuterBoundary.LinearRing.Coordinates);
                coordinates.AddRange(polygon.InnerBoundary.Select(inner => inner.LinearRing.Coordinates.ToList()));
            }
            return coordinates.Select(c => c.ToArray()).ToArray();
        }

        public void ReverseRingOrientation()
        {
            List<Vector> reversedCoordinates = new List<Vector>();
            for (int i = Coordinates.Length - 1; i >= 0; i--)
            {
                reversedCoordinates.Add(Coordinates[i]);
            }
            Coordinates = reversedCoordinates.ToArray();
        }

        public override string ToString()
        {
            return Name + " " + Id + " - " + ShapeType;
        }

        public SqlCommand GetSqlCommand()
        {
            return MapFeatureCommandCreator.CreateCommand(this, _configuration);
        }

        internal string GetInsertQuery(bool declareVariables = true)
        {
            return MapFeatureCommandCreator.CreateCommandQuery(this, _configuration, false, declareVariables);
        }

        public string GetInsertQuery()
        {
            return MapFeatureCommandCreator.CreateCommandQuery(this, _configuration, false, true);
        }
    }
}
