using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class SchemaRegistryClient
        : ISchemaRegistryClient
    {
        private const string _clientName = "SchemaRegistryClient";

        private readonly global::StrawberryShake.IOperationExecutor _executor;
        private readonly global::StrawberryShake.IOperationStreamExecutor _streamExecutor;

        public SchemaRegistryClient(global::StrawberryShake.IOperationExecutorPool executorPool)
        {
            _executor = executorPool.CreateExecutor(_clientName);
            _streamExecutor = executorPool.CreateStreamExecutor(_clientName);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IPublishSchema>> PublishSchemaAsync(
            global::StrawberryShake.Optional<string> externalId = default,
            global::StrawberryShake.Optional<string> schemaName = default,
            global::StrawberryShake.Optional<string> environmentName = default,
            global::StrawberryShake.Optional<string?> sourceText = default,
            global::StrawberryShake.Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.TagInput>?> tags = default,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (externalId.HasValue && externalId.Value is null)
            {
                throw new ArgumentNullException(nameof(externalId));
            }

            if (schemaName.HasValue && schemaName.Value is null)
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if (environmentName.HasValue && environmentName.Value is null)
            {
                throw new ArgumentNullException(nameof(environmentName));
            }

            return _executor.ExecuteAsync(
                new PublishSchemaOperation
                {
                    ExternalId = externalId, 
                    SchemaName = schemaName, 
                    EnvironmentName = environmentName, 
                    SourceText = sourceText, 
                    Tags = tags
                },
                cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IPublishSchema>> PublishSchemaAsync(
            PublishSchemaOperation operation,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IMarkSchemaPublished>> MarkSchemaPublishedAsync(
            global::StrawberryShake.Optional<string> externalId = default,
            global::StrawberryShake.Optional<string> schemaName = default,
            global::StrawberryShake.Optional<string> environmentName = default,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (externalId.HasValue && externalId.Value is null)
            {
                throw new ArgumentNullException(nameof(externalId));
            }

            if (schemaName.HasValue && schemaName.Value is null)
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if (environmentName.HasValue && environmentName.Value is null)
            {
                throw new ArgumentNullException(nameof(environmentName));
            }

            return _executor.ExecuteAsync(
                new MarkSchemaPublishedOperation
                {
                    ExternalId = externalId, 
                    SchemaName = schemaName, 
                    EnvironmentName = environmentName
                },
                cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IMarkSchemaPublished>> MarkSchemaPublishedAsync(
            MarkSchemaPublishedOperation operation,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IPublishClient>> PublishClientAsync(
            global::StrawberryShake.Optional<string> externalId = default,
            global::StrawberryShake.Optional<string> schemaName = default,
            global::StrawberryShake.Optional<string> environmentName = default,
            global::StrawberryShake.Optional<string> clientName = default,
            global::StrawberryShake.Optional<QueryFileFormat> format = default,
            global::StrawberryShake.Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.QueryFileInput>> files = default,
            global::StrawberryShake.Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.TagInput>?> tags = default,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (externalId.HasValue && externalId.Value is null)
            {
                throw new ArgumentNullException(nameof(externalId));
            }

            if (schemaName.HasValue && schemaName.Value is null)
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if (environmentName.HasValue && environmentName.Value is null)
            {
                throw new ArgumentNullException(nameof(environmentName));
            }

            if (clientName.HasValue && clientName.Value is null)
            {
                throw new ArgumentNullException(nameof(clientName));
            }

            if (files.HasValue && files.Value is null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            return _executor.ExecuteAsync(
                new PublishClientOperation
                {
                    ExternalId = externalId, 
                    SchemaName = schemaName, 
                    EnvironmentName = environmentName, 
                    ClientName = clientName, 
                    Format = format, 
                    Files = files, 
                    Tags = tags
                },
                cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IPublishClient>> PublishClientAsync(
            PublishClientOperation operation,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IMarkClientPublished>> MarkClientPublishedAsync(
            global::StrawberryShake.Optional<string> externalId = default,
            global::StrawberryShake.Optional<string> schemaName = default,
            global::StrawberryShake.Optional<string> environmentName = default,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (externalId.HasValue && externalId.Value is null)
            {
                throw new ArgumentNullException(nameof(externalId));
            }

            if (schemaName.HasValue && schemaName.Value is null)
            {
                throw new ArgumentNullException(nameof(schemaName));
            }

            if (environmentName.HasValue && environmentName.Value is null)
            {
                throw new ArgumentNullException(nameof(environmentName));
            }

            return _executor.ExecuteAsync(
                new MarkClientPublishedOperation
                {
                    ExternalId = externalId, 
                    SchemaName = schemaName, 
                    EnvironmentName = environmentName
                },
                cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<global::StrawberryShake.IMarkClientPublished>> MarkClientPublishedAsync(
            MarkClientPublishedOperation operation,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IResponseStream<global::StrawberryShake.IOnPublishDocument>> OnPublishDocumentAsync(
            global::StrawberryShake.Optional<string> sessionId = default,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (sessionId.HasValue && sessionId.Value is null)
            {
                throw new ArgumentNullException(nameof(sessionId));
            }

            return _streamExecutor.ExecuteAsync(
                new OnPublishDocumentOperation { SessionId = sessionId },
                cancellationToken);
        }

        public global::System.Threading.Tasks.Task<global::StrawberryShake.IResponseStream<global::StrawberryShake.IOnPublishDocument>> OnPublishDocumentAsync(
            OnPublishDocumentOperation operation,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _streamExecutor.ExecuteAsync(operation, cancellationToken);
        }
    }
}
