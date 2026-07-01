using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveNames = HotChocolate.Types.DirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal class RequiresOptInDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.RequiresOptIn.Name;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(MutableSchemaDefinition schema)
    {
        return RequiresOptInMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<DirectivesProviderInfo> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        if (!mergedSchema.DirectiveDefinitions.TryGetDirective(DirectiveName, out var directiveDefinition))
        {
            // Merged definition not found.
            return;
        }

        var uniqueFeatures =
            memberDefinitions
                .SelectMany(d => d.Member.Directives.Where(dir => dir.Name == DirectiveNames.RequiresOptIn.Name))
                .Select(RequiresOptInDirective.From)
                .Select(d => d.Feature)
                .Distinct();

        foreach (var feature in uniqueFeatures)
        {
            mergedMember.AddDirective(
                new Directive(
                    directiveDefinition,
                    new ArgumentAssignment(
                        DirectiveNames.RequiresOptIn.Arguments.Feature,
                        feature)));
        }
    }
}
