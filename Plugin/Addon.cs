using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniversalAssetLoader.AddonLibrary
{
    class Addon
    {
        public Addon(string configPath = "") { path = configPath; }
        public string name { get; set; }
        public string description { get; set; }

        public string[] target { get; set; }
        [JsonIgnore]
        public Texture2D preview_texture { get; set; }
        public string preview { get; set; }
        public string type { get; set; }
        public string model_type { get; set; }
        public TransformData transform { get; set; }
        [JsonPropertyName("override")]
        public TransformData[] transform_override { get; set; }
        public SimulationArgs simulation_args { get; set; }
        [JsonIgnore]
        public string path { get; set; }
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Addon Information ===");
            sb.AppendLine($"Name: {name ?? "N/A"}");
            sb.AppendLine($"Description: {description ?? "N/A"}");
            sb.AppendLine($"Type: {type ?? "N/A"}");
            sb.AppendLine($"Model Type: {model_type ?? "N/A"}");
            sb.AppendLine($"Path: {path ?? "N/A"}");

            // Print targets
            if (target != null && target.Length > 0)
            {
                sb.AppendLine("Targets: " + string.Join(", ", target));
            }
            else
            {
                sb.AppendLine("Targets: None");
            }


            sb.AppendLine($"Transform: {transform.ToString()}");


            // Print Transform Overrides
            if (transform_override != null && transform_override.Length > 0)
            {
                sb.AppendLine("Transform Overrides:");
                for (int i = 0; i < transform_override.Length; i++)
                {
                    sb.AppendLine($"  [{i}] {transform_override[i]}");
                }
            }
            else
            {
                sb.AppendLine("Transform Overrides: None");
            }

            // Preview

            sb.AppendLine($"Preview: {model_type ?? "N/A"}");

            sb.AppendLine("==========================");
            return sb.ToString();
        }


        public static Addon Deserialize(Addon addon)
        {
            string path = addon.path;
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            // Add the custom Vector3 converter
            options.Converters.Add(new Utils.Vector3JsonConverter());
            options.Converters.Add(new SimulationArgsConverter());

            try
            {
                addon = JsonSerializer.Deserialize<Addon>(json, options);
                addon.path = path;
                if (addon != null)
                {
                    return addon;
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Deserialization failed for file: {path}\nError: {ex.Message}");
            }

            return null;
        }

    }

    




}
