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


        [TestInitialize]
        public void InitializeTests()
        {
            myUploader = new MapUploader(ConfigurationManager.ConnectionStrings["TestDb"].ToString());
        }

        [TestMethod]
        public void CheckNPA()
        {
            myUploader.Upload("polygon", @"TestData\npa.kml", tableName + "NPA", 4326, true);
        }

        [TestMethod]
        public void BasicKML()
        {
            myUploader.Upload( "polygon", @"TestData\Basic.kml", tableName + "Basic", 4326, true);
        }

        [TestMethod]
        public void BasicKMLGeometry()
        {
            myUploader.Upload("polygon", @"TestData\Basic.kml", tableName + "BasicGeom", 4326, false);
        }

        [TestMethod]
        public void CheckNPAGeometry()
        {
            myUploader.Upload("polygon", @"TestData\npa.kml", tableName + "NPAGeom", 4326, false);
        }

        [TestMethod]
        public void SchoolTest()
        {
            myUploader.Upload( "polygon", @"TestData\school.kml", tableName + "School", 4326, true);
        }

        [TestMethod]
        public void SchoolTestGeometry()
        {
            myUploader.Upload("polygon", @"TestData\school.kml", tableName + "SchoolGeom", 4326, false);
        }

        [TestMethod]
        public void GoogleSample()
        {
            myUploader.Upload("polygon", @"TestData\KML_Samples.kml", tableName + "Google", 4326, false);
        }

        [TestMethod]
        public void UsZips()
        {
            myUploader.Upload("polygon", @"TestData\us_zips.kml", tableName + "Zips", 4326, false, true);
        }

        //[TestMethod]
        //public void BasicKmlOnMySql()
        //{
        //    myUploader = new MapUploader("192.168.0.202", "test", "root", passwordList[1], "placemark", @"TestData\Basic.kml", myInfo.Table, 4326, true);
        //    myUploader.Upload();
        //}
    }
}
