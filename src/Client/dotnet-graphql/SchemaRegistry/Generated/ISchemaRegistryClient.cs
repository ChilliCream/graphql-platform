using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface ISchemaRegistryClient
    {
        Task<IOperationResult<IPublishSchema>> PublishSchemaAsync(
            Optional<string> schemaName = default,
            Optional<string> environmentName = default,
            Optional<string> sourceText = default,
            Optional<IReadOnlyList<TagInput>?> tags = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<IPublishSchema>> PublishSchemaAsync(
            PublishSchemaOperation operation,
            CancellationToken cancellationToken = default);
    }
}
