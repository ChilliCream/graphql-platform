using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.VisualStudio
{
    public partial class GraphQLLanguageClient
    {
        private static async Task BuildServerConfigAsync(string rootDirectory)
        {
            await Task.Yield();

            string serverConfigFileName = Path.Combine(rootDirectory, ".graphqlrc.json");

            if (File.Exists(serverConfigFileName))
            {
                File.Delete(serverConfigFileName);
            }

            var serverConfig = new ServerConfig();

            foreach(string fileName in Directory.GetFiles(rootDirectory, ".graphqlrc.json", SearchOption.AllDirectories))
            {
                string json = File.ReadAllText(fileName);
                string path = Normalize(rootDirectory, Path.GetDirectoryName(fileName));
                GraphQLConfig config = GraphQLConfig.FromJson(json);
                var project = new ServerConfigProject();
                project.Schema.Add($"{path}/{config.Schema}");
                project.Documents.Add($"{path}/{config.Documents}");
                serverConfig.Projects.Add(project);
            }

            File.WriteAllText(
                serverConfigFileName,
                JsonConvert.SerializeObject(serverConfig, CreateJsonSettings()));
        }

        private static string Normalize(string rootDirectory, string configDirectory)
        {
            return configDirectory.Substring(rootDirectory.Length).Replace("\\", "/").Trim('/');
        }

        private static JsonSerializerSettings CreateJsonSettings()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };

            jsonSettings.Converters.Add(new StringEnumConverter());

            return jsonSettings;
        }

        private class ServerConfig
        {
            public List<ServerConfigProject> Projects { get; } = new List<ServerConfigProject>();
        }

        public class ServerConfigProject
        {
            public List<string> Schema { get; } = new List<string>();

            public List<string> Documents { get; } = new List<string>();
        }
    }
}
