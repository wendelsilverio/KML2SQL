using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;

namespace KML2SQL
{
    static class SettingsPersister
    {
        private static string FileName = Path.Combine(Utility.GetApplicationFolder(), "KML2SQL.settings");

        public static void Persist(Settings settings)
        {
            var settingsText = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
            File.WriteAllText(FileName, settingsText);
        }
        public static Settings Retrieve()
        {

            if (File.Exists(FileName))
            {
                var settingsText = File.ReadAllText(FileName);
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(settingsText);
                return settings;
            }
            return null;
        }

    }
}