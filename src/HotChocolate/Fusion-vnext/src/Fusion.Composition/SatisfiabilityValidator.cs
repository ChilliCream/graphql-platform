using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

#pragma warning disable CS9113 // Parameter is unread.
internal sealed class SatisfiabilityValidator(SchemaDefinition mergedSchema)
#pragma warning restore CS9113 // Parameter is unread.
{
    public CompositionResult Validate()
    {
        // FIXME: Implement.
        return CompositionResult.Success();
    }
}
