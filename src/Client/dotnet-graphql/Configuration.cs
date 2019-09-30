using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace StrawberryShake.Tools
{
    public class Configuration
    {
        public List<SchemaFile>? Schemas { get; set; }

        public string? ClientName { get; set; }

        public static async Task<Configuration?> LoadConfig(string path)
        {
            Configuration config;

            using (var stream = File.OpenRead(Path.Combine(path, WellKnownFiles.Config)))
            {
                config = await JsonSerializer.DeserializeAsync<Configuration>(
                    stream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
            }

            return config;
        }

        public static bool Exists(string path)
        {
            return File.Exists(Path.Combine(path, WellKnownFiles.Config));
        }
    }
}
