using System.Collections.Immutable;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.DirectiveMergers;

internal interface IDirectiveMerger
{
    string DirectiveName { get; }

    DirectiveMergeBehavior MergeBehavior { get; }

    MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema);

    void MergeDirectiveDefinition(
        MutableDirectiveDefinition directiveDefinition,
        MutableSchemaDefinition mergedSchema);

    void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<IDirectivesProvider> memberDefinitions,
        MutableSchemaDefinition mergedSchema);
}
