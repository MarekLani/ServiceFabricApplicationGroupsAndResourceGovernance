using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdminAppCore
{
    class Settings
    {

        public string clientCertThumb { get; set; }
        public  string serverCertThumb { get; set; } 
        public  string CommonName { get; set; }
        public  string connection { get; set; }

        public  string appName { get; set; }
        public string appTypeName { get; set; }
        public  string appVersion { get; set; }

        public static Settings Instance { get; set; } = new Settings();

        public static void SaveSettings()
        {
            var settings = JsonConvert.SerializeObject(Instance);
            System.IO.File.WriteAllText(@"settings.json", settings);
        }

        public static void LoadSettings()
        {
            if (!System.IO.File.Exists("settings.json"))
            {
                System.IO.File.Create("settings.json");
         
            }

            var settings = System.IO.File.ReadAllText("settings.json");
            Settings.Instance = JsonConvert.DeserializeObject<Settings>(settings);

        }

    }
}
