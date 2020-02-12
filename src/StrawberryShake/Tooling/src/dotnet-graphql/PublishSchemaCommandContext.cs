using System.Collections.Generic;
using System;
using StrawberryShake.Tools.SchemaRegistry;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandContext
    {
        public PublishSchemaCommandContext(
            Uri registry,
            string externalId,
            string schemaName,
            string environmentName,
            string schemaFileName,
            IReadOnlyList<TagInput>? tags,
            string? token,
            string? scheme)
        {
            Registry = registry;
            ExternalId = externalId;
            SchemaName = schemaName;
            EnvironmentName = environmentName;
            SchemaFileName = schemaFileName;
            Tags = tags;
            Token = token;
            Scheme = scheme;
        }

        public Uri Registry { get; }
        public string ExternalId { get; }
        public string SchemaName { get; }
        public string EnvironmentName { get; }
        public string SchemaFileName { get; }
        public IReadOnlyList<TagInput>? Tags { get; }
        public string? Token { get; }
        public string? Scheme { get; }
    }
}
