using System;
using System.Collections.Generic;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.Tools
{
    public class InitCommandContext
    {
        public InitCommandContext(
            string name,
            string path,
            Uri uri,
            string? token,
            string? scheme,
            Dictionary<string, IEnumerable<string>> customHeaders)
        {
            SchemaName = "Schema";
            SchemaFileName = FileNames.SchemaFile;
            SchemaExtensionFileName = FileNames.SchemaExtensionFile;
            ClientName = name;
            Path = path;
            Uri = uri;
            Token = token;
            Scheme = scheme;
            CustomHeaders = customHeaders;
        }

        public string SchemaName { get; }
        public string SchemaFileName { get; }
        public string SchemaExtensionFileName { get; }
        public string ConfigFileName => FileNames.GraphQLConfigFile;
        public string ClientName { get; }
        public string Path { get; }
        public Uri? Uri { get; }
        public string? Token { get; }
        public string? Scheme { get; }
        public Dictionary<string, IEnumerable<string>> CustomHeaders { get; }
        public string? CustomNamespace { get; set; }
        public bool UseDependencyInjection { get; set; } = true;
    }
}
