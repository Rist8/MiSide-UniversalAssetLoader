using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using UnityEngine;
using System.Threading.Tasks;

namespace UniversalAssetLoader.Utils
{
    class TransformData
    {
        public string name { get; set; }
        public Vector3Data position { get; set; }
        public Vector3Data rotation { get; set; }
        public Vector3Data scale { get; set; }

        public override string ToString()
        {
            return $"Position: {position}, Rotation: {rotation}, Scale: {scale}";
        }
    }

    public struct Vector3Data
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public override string ToString()
        {
            return $"(x: {x}, y: {y}, z: {z})";
        }

        public static implicit operator Vector3(Vector3Data v) => new Vector3(v.x, v.y, v.z);
    }
    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the start object token
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            float x = 0, y = 0, z = 0;

            // Read each property
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            x = (float)reader.GetDouble();
                            break;
                        case "y":
                            y = (float)reader.GetDouble();
                            break;
                        case "z":
                            z = (float)reader.GetDouble();
                            break;
                        default:
                            throw new JsonException($"Unexpected property {propertyName}");
                    }
                }
            }

            return new Vector3(x, y, z);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }
    internal static class Utility
    {
    }
}
