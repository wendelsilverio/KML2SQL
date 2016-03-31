using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kml2Sql.Mapping
{
    public class Kml2SqlConfig
    {
        public string TableName { get; set; } = "KmlUpload";
        public string PlacemarkColumnName { get; set; } = "Placemark";
        public string IdColumnName { get; set; } = "Id";
        public PolygonType GeoType { get; set; }
        public bool MakeValid { get; set; } = true;
        public bool FixPolygons { get; set; }
        public int Srid { get; set; } = 4326;

        private Dictionary<string, string> ColumnNameMap = new Dictionary<string, string>();

        public void MapColumnName(string placemarkName, string columnName)
        {
            ColumnNameMap.Add(placemarkName.ToLower(), columnName);
        }

        internal string GetColumnName(string placemarkName)
        {
            var key = placemarkName.ToLower();
            if (ColumnNameMap.ContainsKey(key))
            {
                return ColumnNameMap[key];
            }
            return placemarkName;
        }
    }
}
