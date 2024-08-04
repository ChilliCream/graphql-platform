using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Completion;
using HotChocolate.Fusion.Planning.Directives;

namespace HotChocolate.Fusion.Planning;

public class SourceObjectField(
    string name,
    string schemaName)
    : ISourceField
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public ImmutableArray<Requirement> Requirements { get; private set; }

    public ICompositeType Type { get; private set; } = default!;

    internal void Complete(SourceObjectFieldCompletionContext context)
    {
        Requirements = context.Requirements;
        Type = context.Type;
    }
}
