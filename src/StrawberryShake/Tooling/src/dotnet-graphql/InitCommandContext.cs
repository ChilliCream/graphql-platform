using System;

namespace StrawberryShake.Tools
{
    public class InitCommandContext
    {
        public InitCommandContext(
            string schemaName,
            string path,
            Uri uri,
            string? token,
            string? scheme)
        {
            SchemaName = schemaName;
            SchemaFileName = schemaName + ".graphql";
            SchemaExtensionFileName = schemaName + "extensions.graphql";
            ClientName = schemaName + "Client";
            ClientName = schemaName + "Client";
            Path = path;
            Uri = uri;
            Token = token;
            Scheme = scheme;
        }

        public InitCommandContext(
            string schemaName,
            string path,
            string? token,
            string? scheme)
        {
            SchemaName = schemaName;
            SchemaFileName = schemaName + ".graphql";
            SchemaExtensionFileName = schemaName + "extensions.graphql";
            ClientName = schemaName + "Client";
            Path = path;
            Uri = null;
            Token = token;
            Scheme = scheme;
        }

        public string SchemaName { get; }
        public string SchemaFileName { get; }
        public string SchemaExtensionFileName { get; }
        public string ConfigFileName { get; } = ".graphqlrc.json";
        public string ClientName { get; }
        public string Path { get; }
        public Uri? Uri { get; }
        public string? Token { get; }
        public string? Scheme { get; }
        public string? CustomNamespace { get; set; }
        public bool UseDependencyInjection { get; set; } = true;
    }
}
