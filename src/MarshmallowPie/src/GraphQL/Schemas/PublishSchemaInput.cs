using System.Collections.Generic;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class PublishSchemaInput
    {
        public PublishSchemaInput(
            string environmentName,
            string schemaName,
            string sourceText,
            IReadOnlyList<TagInput>? tags,
            string? clientMutationId)
        {
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            SourceText = sourceText;
            Tags = tags;
            ClientMutationId = clientMutationId;
        }

        public string EnvironmentName { get; }

        public string SchemaName { get; }

        public string SourceText { get; }

        public IReadOnlyList<TagInput>? Tags { get; }

        public string? ClientMutationId { get; }
    }
}
