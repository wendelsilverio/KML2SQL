using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kml2Sql.MsSql
{
    public enum ShapeType
    {
        Point,
        Polygon,
        LineString
    }

    public enum GeoType
    {
        Geometry,
        Geography
    }
}
