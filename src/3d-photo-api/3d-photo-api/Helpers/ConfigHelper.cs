using System.Collections.Generic;
using System.IO;

namespace photo_api.Helpers
{
    public class ConfigHelper
    {
        private static string DefaultConfigFile = Startup.Configuration["AppSettings:DefaultConfigFile"];

        public static void WriteConfig(string outputFile, Dictionary<string, object> settingsOverride)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            var configYaml = File.ReadAllText(DefaultConfigFile);
            var config = deserializer.Deserialize<Dictionary<string, object>>(configYaml);

            foreach(var s in settingsOverride)
            {
                config[s.Key] = s.Value;
            }

            var serializer = new YamlDotNet.Serialization.SerializerBuilder().Build();
            string newConfigYaml = null;
            try
            {
                newConfigYaml = serializer.Serialize(config);
            }
            catch (System.Exception ex)
            {
                var a = ex;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            File.WriteAllText(outputFile, newConfigYaml);

        }
    }
}
