using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kml2Sql.Mapping
{
    public static class ExtensionMethods
    {
        public static string Sanitize(this string myString)
        {
            return myString.Replace("--", "").Replace(";", "").Replace("'", "\"");
        }
    }
}
