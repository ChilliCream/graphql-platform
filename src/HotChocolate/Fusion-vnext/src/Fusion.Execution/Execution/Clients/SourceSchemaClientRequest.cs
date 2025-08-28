using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaClientRequest
{
    public required OperationType OperationType { get; init; }

    public required string OperationSourceText { get; init; }

    public ImmutableArray<VariableValues> Variables { get; init; } = [];
}
