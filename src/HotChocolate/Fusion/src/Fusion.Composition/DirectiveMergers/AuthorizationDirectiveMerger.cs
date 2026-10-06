using System.Collections.Immutable;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal sealed class AuthorizationDirectiveMerger(MutableDirectiveDefinition fusionAuthorization)
    : DirectiveMergerBase(DirectiveMergeBehavior.Ignore)
{
    public override string DirectiveName => DirectiveNames.FusionAuthorization;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(MutableSchemaDefinition schema)
    {
        return fusionAuthorization;
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<DirectivesProviderInfo> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        var merged = AuthorizationGroups.Merge(memberDefinitions);

        if (merged.IsEmpty)
        {
            return;
        }

        var arguments = new List<ArgumentAssignment>();

        if (merged.Authenticated)
        {
            arguments.Add(new ArgumentAssignment(ArgumentNames.Authenticated, true));
        }

        if (!merged.Scopes.IsEmpty)
        {
            arguments.Add(new ArgumentAssignment(ArgumentNames.Scopes, ToValueNode(merged.Scopes)));
        }

        if (!merged.Policies.IsEmpty)
        {
            arguments.Add(new ArgumentAssignment(ArgumentNames.Policies, ToValueNode(merged.Policies)));
        }

        mergedMember.AddDirective(new Directive(fusionAuthorization, arguments));
    }

    private static ListValueNode ToValueNode(ImmutableArray<ImmutableArray<string>> groups)
    {
        var items = new List<IValueNode>(groups.Length);

        foreach (var group in groups)
        {
            var values = new List<IValueNode>(group.Length);

            foreach (var value in group)
            {
                values.Add(new StringValueNode(value));
            }

            items.Add(new ListValueNode(values));
        }

        return new ListValueNode(items);
    }
}
