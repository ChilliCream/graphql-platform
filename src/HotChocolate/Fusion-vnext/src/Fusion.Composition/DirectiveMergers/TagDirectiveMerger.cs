using System.Collections.Immutable;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Definitions;
using HotChocolate.Types.Mutable.Directives;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal class TagDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName { get; } =
        mergeBehavior is DirectiveMergeBehavior.IncludePrivate
            ? $"fusion__{DirectiveNames.Tag}"
            : DirectiveNames.Tag;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema)
    {
        return TagMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<IDirectivesProvider> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        if (MergeBehavior is DirectiveMergeBehavior.Ignore)
        {
            return;
        }

        if (!mergedSchema.DirectiveDefinitions.TryGetDirective(DirectiveName, out var directiveDefinition))
        {
            // Merged definition not found.
            return;
        }

        var uniqueTagDirectives =
            memberDefinitions
                .SelectMany(d => d.Directives.Where(dir => dir.Name == DirectiveNames.Tag))
                .Select(TagDirective.From)
                .DistinctBy(d => d.Name);

        foreach (var uniqueTagDirective in uniqueTagDirectives)
        {
            var tagDirective =
                new Directive(
                    directiveDefinition,
                    new ArgumentAssignment(ArgumentNames.Name, uniqueTagDirective.Name));

            mergedMember.AddDirective(tagDirective);
        }
    }
}
