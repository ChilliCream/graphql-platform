using System.Collections.Generic;
using System;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandContext
    {
        public PublishSchemaCommandContext(
            Uri registry,
            string schemaName,
            string environmentName,
            string schemaFileName,
            IReadOnlyDictionary<string, string> tags,
            string? token,
            string scheme)
        {
            Registry = registry;
            SchemaName = schemaName;
            EnvironmentName = environmentName;
            SchemaFileName = schemaFileName;
            Tags = tags;
            Token = token;
            Scheme = scheme;
        }

        public Uri Registry { get; }
        public string SchemaName { get; }
        public string EnvironmentName { get; }
        public string SchemaFileName { get; }
        public IReadOnlyDictionary<string, string> Tags { get; }
        public string? Token { get; }
        public string Scheme { get; }
    }
}
