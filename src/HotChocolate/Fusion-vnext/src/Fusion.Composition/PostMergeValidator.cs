using System.Collections.Immutable;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

#pragma warning disable CS9113 // Parameter is unread.
internal sealed class PostMergeValidator(
    SchemaDefinition mergedSchema,
    ImmutableArray<object> rules)
#pragma warning restore CS9113 // Parameter is unread.
{
    public CompositionResult Validate()
    {
        // FIXME: Implement.
        return CompositionResult.Success();
    }
}
