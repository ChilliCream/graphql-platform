using System.Collections.Immutable;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

internal sealed class PostMergeValidator(IEnumerable<object> rules)
{
    private readonly ImmutableArray<object> _rules = [.. rules];

    public CompositionResult Validate(SchemaDefinition _)
    {
        // FIXME: Implement.
        return CompositionResult.Success();
    }
}
