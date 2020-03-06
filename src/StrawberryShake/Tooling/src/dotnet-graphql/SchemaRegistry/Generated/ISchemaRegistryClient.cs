using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial interface ISchemaRegistryClient
    {
        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IPublishSchema>> PublishSchemaAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            Optional<string> sourceText = default,
            Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.Tools.SchemaRegistry.TagInput>?> tags = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IPublishSchema>> PublishSchemaAsync(
            PublishSchemaOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IMarkSchemaPublished>> MarkSchemaPublishedAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IMarkSchemaPublished>> MarkSchemaPublishedAsync(
            MarkSchemaPublishedOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IPublishClient>> PublishClientAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            Optional<string> clientName = default,
            Optional<QueryFileFormat> format = default,
            Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.Tools.SchemaRegistry.QueryFileInput>> files = default,
            Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.Tools.SchemaRegistry.TagInput>?> tags = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IPublishClient>> PublishClientAsync(
            PublishClientOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IMarkClientPublished>> MarkClientPublishedAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.Tools.SchemaRegistry.IMarkClientPublished>> MarkClientPublishedAsync(
            MarkClientPublishedOperation operation,
            CancellationToken cancellationToken = default);

        global::System.Threading.Tasks.Task<global::StrawberryShake.IResponseStream<global::StrawberryShake.Tools.SchemaRegistry.IOnPublishDocument>> OnPublishDocumentAsync(
            Optional<string> sessionId = default,
            CancellationToken cancellationToken = default);

        global::System.Threading.Tasks.Task<global::StrawberryShake.IResponseStream<global::StrawberryShake.Tools.SchemaRegistry.IOnPublishDocument>> OnPublishDocumentAsync(
            OnPublishDocumentOperation operation,
            CancellationToken cancellationToken = default);
    }
}
