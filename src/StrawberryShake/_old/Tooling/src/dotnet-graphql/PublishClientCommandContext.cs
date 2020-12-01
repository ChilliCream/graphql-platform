using System.Collections.Generic;
using System;
using StrawberryShake.Tools.SchemaRegistry;

namespace StrawberryShake.Tools
{
    public class PublishClientCommandContext
    {
        public PublishClientCommandContext(
            Uri registry,
            string environmentName,
            string schemaName,
            string clientName,
            string externalId,
            string searchDirectory,
            IReadOnlyList<string>? queryFileNames,
            bool relayFileFormat,
            IReadOnlyList<TagInput>? tags,
            bool published,
            string? token,
            string? scheme)
        {
            Registry = registry;
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            ClientName = clientName;
            ExternalId = externalId;
            SearchDirectory = searchDirectory;
            QueryFileNames = queryFileNames;
            RelayFileFormat = relayFileFormat;
            Tags = tags;
            Published = published;
            Token = token;
            Scheme = scheme;
        }

        public Uri Registry { get; }

        public string EnvironmentName { get; }

        public string SchemaName { get; }

        public string ClientName { get; }

        public string ExternalId { get; }

        public string SearchDirectory { get; }

        public IReadOnlyList<string>? QueryFileNames { get; }

        public bool RelayFileFormat { get; }

        public IReadOnlyList<TagInput>? Tags { get; }

        public bool Published { get; }

        public string? Token { get; }

        public string? Scheme { get; }
    }
}
