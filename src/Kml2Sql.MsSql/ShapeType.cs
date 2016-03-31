using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kml2Sql.Mapping
{
    public enum ShapeType
    {
        Point,
        Polygon,
        LineString
    }

    public enum PolygonType
    {
        Geometry,
        Geography
    }
}
