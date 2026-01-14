using System.Collections.Immutable;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.DirectiveMergers;

internal abstract class DirectiveMergerBase(DirectiveMergeBehavior mergeBehavior) : IDirectiveMerger
{
    public abstract string DirectiveName { get; }

    public DirectiveMergeBehavior MergeBehavior { get; } = mergeBehavior;

    public abstract MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema);

    public virtual void MergeDirectiveDefinition(
        MutableDirectiveDefinition directiveDefinition,
        MutableSchemaDefinition mergedSchema)
    {
        if (MergeBehavior is DirectiveMergeBehavior.IncludePrivate)
        {
            directiveDefinition.Name = DirectiveName;
        }

        mergedSchema.DirectiveDefinitions.Add(directiveDefinition);
    }

    public abstract void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<IDirectivesProvider> memberDefinitions,
        MutableSchemaDefinition mergedSchema);
}
