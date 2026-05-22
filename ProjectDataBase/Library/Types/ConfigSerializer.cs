using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace ProjectDataBase.Library.Types
{
    public static class ConfigSerializer
    {
        private static readonly JsonSerializerSettings Settings =
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

        public static void Save(string path, ConfigRoot config)
        {
            string json = JsonConvert.SerializeObject(config, Settings);

            File.WriteAllText(path, json);
        }

        public static ConfigRoot Load(string path)
        {
            if (!File.Exists(path))
                return new ConfigRoot();

            string json = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<ConfigRoot>(json)
                   ?? new ConfigRoot();
        }
    } 

    public class ConfigRoot
    {
        [JsonProperty("config")]
        public ConfigData Config { get; set; } = new ConfigData();
    }

    public class ConfigData
    {
        [JsonProperty("properties")]
        public List<CategoryConfig> Properties { get; set; } = new List<CategoryConfig>();
    }

    public class CategoryConfig
    {
        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("properties")]
        public List<PropertyConfig> Properties { get; set; } = new List<PropertyConfig>();
    }

    public class PropertyConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("conversion")]
        public string Conversion { get; set; } = string.Empty;
    }
}