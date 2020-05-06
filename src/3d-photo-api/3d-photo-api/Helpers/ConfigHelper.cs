using System.Collections.Generic;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace photo_api.Helpers
{
    public class ConfigHelper
    {
        private static string DefaultConfigFile = Startup.Configuration["AppSettings:DefaultConfigFile"];

        public static void WriteConfig(string outputFile, Dictionary<string, object> settingsOverride)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .Build();
            var configYaml = File.ReadAllText(DefaultConfigFile);
            var config = deserializer.Deserialize<Dictionary<string, object>>(configYaml);

            foreach(var s in settingsOverride)
            {
                config[s.Key] = s.Value;
            }

            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                .WithEventEmitter(next => new FlowStyleIntegerSequences(next))
                .Build();
            string newConfigYaml = null;
            newConfigYaml = serializer.Serialize(config);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            File.WriteAllText(outputFile, newConfigYaml);

        }
    }

    public class FlowStyleIntegerSequences : ChainedEventEmitter
    {
        public FlowStyleIntegerSequences(IEventEmitter nextEmitter)
            : base(nextEmitter) { }

        public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            if (typeof(IEnumerable<object>).IsAssignableFrom(eventInfo.Source.Type))
            {
                eventInfo = new SequenceStartEventInfo(eventInfo.Source)
                {
                    Style = SequenceStyle.Flow
                };
            }

            nextEmitter.Emit(eventInfo, emitter);
        }
    }
}
