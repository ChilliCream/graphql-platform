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

internal class OptInFeatureStabilityDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.OptInFeatureStability.Name;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(MutableSchemaDefinition schema)
    {
        return OptInFeatureStabilityMutableDirectiveDefinition.Create(schema);
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

        var uniqueByFeature =
            memberDefinitions
                .SelectMany(d => d.Member.Directives.Where(dir => dir.Name == DirectiveNames.OptInFeatureStability.Name))
                .Select(OptInFeatureStabilityDirective.From)
                .DistinctBy(d => d.Feature);

        foreach (var optInFeatureStability in uniqueByFeature)
        {
            mergedMember.AddDirective(
                new Directive(
                    directiveDefinition,
                    new ArgumentAssignment(
                        DirectiveNames.OptInFeatureStability.Arguments.Feature,
                        optInFeatureStability.Feature),
                    new ArgumentAssignment(
                        DirectiveNames.OptInFeatureStability.Arguments.Stability,
                        optInFeatureStability.Stability)));
        }
    }
}
