using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KML2SQL;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.Diagnostics;
using System.Text;
using System.Configuration;
using Kml2Sql.MsSql;
using System.Data.SqlClient;

namespace KML2SQLTests
{
    [TestClass]
    public class Tests
    {
        //====================================================================================================
        //
        // Yes, I know these aren't real tests. Sorry. I wasn't really doing TDD at the time I wrote this.
        //
        //===================================================================================================

        MapUploader myUploader;
        string tableName = "Kml2SqlTest";
        string connectionString;

        [TestInitialize]
        public void InitializeTests()
        {
            connectionString = ConfigurationManager.ConnectionStrings["TestDb"].ToString();
            myUploader = new MapUploader(connectionString);
        }

        [TestMethod]
        public void CheckNPANew()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var config = new Kml2SqlConfig();
                config.MapColumnName("p1", "foo");
                config.MapColumnName("p2", "bar");
                Uploader.DropTable(conn, config.TableName);
                Uploader.Upload(@"TestData\npa.kml", conn, config);
            }
        }

        [TestMethod]
        public void CheckNPA()
        {
            myUploader.Upload("polygon", @"TestData\npa.kml", tableName + "NPA", 4326, GeoType.Geography);
        }

        [TestMethod]
        public void BasicKML()
        {
            myUploader.Upload( "polygon", @"TestData\Basic.kml", tableName + "Basic", 4326, GeoType.Geography);
        }

        [TestMethod]
        public void BasicKMLGeometry()
        {
            myUploader.Upload("polygon", @"TestData\Basic.kml", tableName + "BasicGeom", 4326, GeoType.Geometry);
        }

        [TestMethod]
        public void CheckNPAGeometry()
        {
            myUploader.Upload("polygon", @"TestData\npa.kml", tableName + "NPAGeom", 4326, GeoType.Geometry);
        }

        [TestMethod]
        public void SchoolTest()
        {
            myUploader.Upload( "polygon", @"TestData\school.kml", tableName + "School", 4326, GeoType.Geography);
        }

        [TestMethod]
        public void SchoolTestGeometry()
        {
            myUploader.Upload("polygon", @"TestData\school.kml", tableName + "SchoolGeom", 4326, GeoType.Geometry);
        }

        [TestMethod]
        public void GoogleSample()
        {
            myUploader.Upload("polygon", @"TestData\KML_Samples.kml", tableName + "Google", 4326, GeoType.Geometry);
        }

        //[TestMethod]
        //public void UsZips()
        //{
        //    myUploader.Upload("polygon", @"TestData\us_zips.kml", tableName + "Zips", 4326, GeoType.Geometry, true);
        //}

        //[TestMethod]
        //public void BasicKmlOnMySql()
        //{
        //    myUploader = new MapUploader("192.168.0.202", "test", "root", passwordList[1], "placemark", @"TestData\Basic.kml", myInfo.Table, 4326, true);
        //    myUploader.Upload();
        //}
    }
}
