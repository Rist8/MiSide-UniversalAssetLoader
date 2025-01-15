using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UniversalAssetLoader.AddonLibrary
{
    internal class SimulationArgs
    {
        public string data { get; set; }
    }
    class SimulationArgsConverter : JsonConverter<SimulationArgs>
    {
        public override SimulationArgs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read Start Array
            string content = JsonSerializer.Deserialize<string>(ref reader, options);

            return new SimulationArgs
            {
                data = content
            };
        }

        public override void Write(Utf8JsonWriter writer, SimulationArgs value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.data);
            writer.WriteEndArray();
        }
    }
}
