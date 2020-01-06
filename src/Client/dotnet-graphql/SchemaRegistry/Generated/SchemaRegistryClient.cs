using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class SchemaRegistryClient
        : ISchemaRegistryClient
    {
        private const string _clientName = "SchemaRegistryClient";

        private readonly IOperationExecutor _executor;

        public SchemaRegistryClient(IOperationExecutorPool executorPool)
        {
            _executor = executorPool.CreateExecutor(_clientName);
        }

        public Task<IOperationResult<IPublishSchema>> PublishSchemaAsync(
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            Optional<string> sourceText = default,
            Optional<IReadOnlyList<TagInput>?> tags = default,
            CancellationToken cancellationToken = default)
        {
            if (schemaName.HasValue && schemaName.Value is null)
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if (environmentName.HasValue && environmentName.Value is null)
            {
                throw new ArgumentNullException(nameof(environmentName));
            }

            if (sourceText.HasValue && sourceText.Value is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            return _executor.ExecuteAsync(
                new PublishSchemaOperation
                {
                    SchemaName = schemaName,
                    EnvironmentName = environmentName,
                    SourceText = sourceText,
                    Tags = tags
                },
                cancellationToken);
        }

        public Task<IOperationResult<IPublishSchema>> PublishSchemaAsync(
            PublishSchemaOperation operation,
            CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }
    }
}
