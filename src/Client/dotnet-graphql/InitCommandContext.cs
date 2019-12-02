using System;

namespace StrawberryShake.Tools
{
    public class InitCommandContext
    {
        public InitCommandContext(
            string schemaName,
            string path,
            string? token,
            string scheme,
            Uri uri)
        {
            SchemaName = schemaName;
            SchemaFileName = schemaName + ".graphql";
            ClientName = schemaName + "Client";
            Path = path;
            Token = token;
            Scheme = scheme;
            Uri = uri;
        }

        public string SchemaName { get; }
        public string SchemaFileName { get; }
        public string ClientName { get; }
        public string Path { get; }
        public string? Token { get; }
        public string Scheme { get; }
        public Uri Uri { get; }
    }
}
