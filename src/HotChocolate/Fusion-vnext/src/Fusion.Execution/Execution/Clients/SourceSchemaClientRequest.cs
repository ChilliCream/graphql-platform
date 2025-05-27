using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaClientRequest
{
    public required string OperationId { get; init; }

    public required OperationDefinitionNode Operation { get; init; }

    public ImmutableArray<VariableValues> Variables { get; init; } = [];
}
