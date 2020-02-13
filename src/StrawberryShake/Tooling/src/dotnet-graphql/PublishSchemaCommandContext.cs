using System.Collections.Generic;
using System;
using StrawberryShake.Tools.SchemaRegistry;

namespace StrawberryShake.Tools
{
    public class PublishSchemaCommandContext
    {
        public PublishSchemaCommandContext(
            Uri registry,
            string environmentName,
            string schemaName,
            string externalId,
            string? schemaFileName,
            IReadOnlyList<TagInput>? tags,
            bool published,
            string? token,
            string? scheme)
        {
            Registry = registry;
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            ExternalId = externalId;
            SchemaFileName = schemaFileName;
            Tags = tags;
            Published = published;
            Token = token;
            Scheme = scheme;
        }

        public Uri Registry { get; }

        public string EnvironmentName { get; }

        public string SchemaName { get; }

        public string ExternalId { get; }

        public string? SchemaFileName { get; }

        public IReadOnlyList<TagInput>? Tags { get; }

        public bool Published { get; }

        public string? Token { get; }

        public string? Scheme { get; }
    }
}
