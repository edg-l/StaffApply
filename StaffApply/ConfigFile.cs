using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace StaffApply
{
    public class ConfigFile
    {
        /// <summary>
        /// Gets or sets the ChatFormat.
        /// </summary>
        public string[] AppRanks = {"helper", "mod", "admin"};

        public string[] Questions = { "Why you want to apply?", "You think you are enough mature for this rank?", "How can you help others?", "What would you do if someone breaks a rule?" };

        public int minWords = 50;

        public static ConfigFile Read(string path)
        {
            if (!File.Exists(path))
            {
                ConfigFile config = new ConfigFile();
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                return config;
            }

            return JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(path));
        }
    }
}