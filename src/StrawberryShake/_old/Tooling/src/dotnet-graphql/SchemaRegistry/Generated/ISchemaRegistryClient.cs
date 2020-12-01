using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial interface ISchemaRegistryClient
    {
        Task<IOperationResult<global::StrawberryShake.IPublishSchema>> PublishSchemaAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            Optional<string?> sourceText = default,
            Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.TagInput>?> tags = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.IPublishSchema>> PublishSchemaAsync(
            PublishSchemaOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.IMarkSchemaPublished>> MarkSchemaPublishedAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.IMarkSchemaPublished>> MarkSchemaPublishedAsync(
            MarkSchemaPublishedOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.IPublishClient>> PublishClientAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            Optional<string> clientName = default,
            Optional<QueryFileFormat> format = default,
            Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.QueryFileInput>> files = default,
            Optional<global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.TagInput>?> tags = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.IPublishClient>> PublishClientAsync(
            PublishClientOperation operation,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.IMarkClientPublished>> MarkClientPublishedAsync(
            Optional<string> externalId = default,
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<global::StrawberryShake.IMarkClientPublished>> MarkClientPublishedAsync(
            MarkClientPublishedOperation operation,
            CancellationToken cancellationToken = default);

        global::System.Threading.Tasks.Task<global::StrawberryShake.IResponseStream<global::StrawberryShake.IOnPublishDocument>> OnPublishDocumentAsync(
            Optional<string> sessionId = default,
            CancellationToken cancellationToken = default);

        global::System.Threading.Tasks.Task<global::StrawberryShake.IResponseStream<global::StrawberryShake.IOnPublishDocument>> OnPublishDocumentAsync(
            OnPublishDocumentOperation operation,
            CancellationToken cancellationToken = default);
    }
}
