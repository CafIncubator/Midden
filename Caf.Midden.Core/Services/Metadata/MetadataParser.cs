using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caf.Midden.Core.Services.Metadata
{
    public class MetadataParser
    {
        private readonly IMetadataConverter converter;

        public MetadataParser(IMetadataConverter converter)
        {
            this.converter = converter;
        }

        public Models.v0_1_0alpha4.Metadata Parse(
            string json)
        {
            string version = GetVersion(json);

            Models.v0_1_0alpha4.Metadata result;

            switch(version)
            {
                case "v0.1.0-alpha1":
                case "v0.1.0-alpha2":
                case "v0.1.0-alpha3":
                    result = Deserialize_v0_1_0alpha3(json);
                    break;
                case "v0.1.0-alpha4":
                    result = Deserialize_v0_1_0alpha4(json);
                    break;
                default:
                    throw new ArgumentException("Unable to parse JSON");
            }

            return result;
        }

        private Models.v0_1_0alpha4.Metadata Deserialize_v0_1_0alpha3(
            string json)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                Encoder = System.Text.Encodings.Web
                    .JavaScriptEncoder
                    .UnsafeRelaxedJsonEscaping
            };

            Models.v0_1_0alpha3.Metadata m = 
                JsonSerializer.Deserialize<Models.v0_1_0alpha3.Metadata>(
                    json, options);
            Models.v0_1_0alpha4.Metadata result = 
                converter.Convert(m);

            return result;
        }
        private Models.v0_1_0alpha4.Metadata Deserialize_v0_1_0alpha4(
            string json)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                Encoder = System.Text.Encodings.Web
                    .JavaScriptEncoder
                    .UnsafeRelaxedJsonEscaping
            };

            Models.v0_1_0alpha4.Metadata m = 
                JsonSerializer.Deserialize<Models.v0_1_0alpha4.Metadata>(
                    json, options);
            Models.v0_1_0alpha4.Metadata result = 
                converter.Convert(m);

            return result;
        }

        private string GetVersion(string json)
        {
            var jsonDoc = JsonDocument.Parse(json);

            string version;

            JsonElement jsonElement;
            if (jsonDoc.RootElement.TryGetProperty("file", out jsonElement))
            {
                version = jsonDoc
                    .RootElement
                    .GetProperty("file")
                    .GetProperty("schema-version")
                    .GetString();
            }
            else
            {
                version = jsonDoc
                    .RootElement
                    .GetProperty("schemaVersion")
                    .GetString();
            }

            return version;
        }
    }
}
