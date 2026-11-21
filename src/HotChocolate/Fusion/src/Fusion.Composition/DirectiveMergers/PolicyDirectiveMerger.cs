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

internal sealed class PolicyDirectiveMerger(MutableDirectiveDefinition fusionPolicy)
    : DirectiveMergerBase(DirectiveMergeBehavior.Ignore)
{
    public override string DirectiveName => DirectiveNames.FusionPolicy;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(MutableSchemaDefinition schema)
    {
        return fusionPolicy;
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<DirectivesProviderInfo> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        var merged = Merge(memberDefinitions);

        if (merged.IsEmpty)
        {
            return;
        }

        mergedMember.AddDirective(CreateDirective(fusionPolicy, merged));
    }

    /// <summary>
    /// Creates the <c>@fusion__policy</c> directive that carries <paramref name="merged"/>.
    /// </summary>
    public static Directive CreateDirective(
        MutableDirectiveDefinition fusionPolicy,
        MergedPolicy merged)
    {
        var arguments = new List<ArgumentAssignment>();

        if (merged.Authenticated)
        {
            arguments.Add(new ArgumentAssignment(ArgumentNames.Authenticated, true));
        }

        if (!merged.Scopes.IsEmpty)
        {
            arguments.Add(new ArgumentAssignment(ArgumentNames.Scopes, ToValueNode(merged.Scopes.Groups)));
        }

        if (!merged.Policies.IsEmpty)
        {
            arguments.Add(new ArgumentAssignment(ArgumentNames.Policies, ToValueNode(merged.Policies.Groups)));
        }

        return new Directive(fusionPolicy, arguments);
    }

    /// <summary>
    /// Merges the policy directives of source schema members.
    /// </summary>
    internal static MergedPolicy Merge(IEnumerable<DirectivesProviderInfo> memberDefinitions)
    {
        var authenticated = false;
        var scopes = PolicyRequirement.Empty;
        var policies = PolicyRequirement.Empty;

        foreach (var (member, _) in memberDefinitions)
        {
            foreach (var directive in member.Directives)
            {
                switch (directive.Name)
                {
                    case DirectiveNames.Authenticated:
                        authenticated = true;
                        break;

                    case DirectiveNames.RequiresScopes:
                        scopes = scopes.And(PolicyRequirement.Create(ParseGroups(directive, ArgumentNames.Scopes)));
                        break;

                    case DirectiveNames.Policy:
                        policies = policies.And(PolicyRequirement.Create(ParseGroups(directive, ArgumentNames.Policies)));
                        break;
                }
            }
        }

        return new MergedPolicy(authenticated, scopes, policies);
    }

    /// <summary>
    /// Reads the requirement carried by a <c>@fusion__policy</c> directive.
    /// </summary>
    internal static MergedPolicy FromFusionDirective(IDirective directive)
    {
        var authenticated = directive.Arguments.TryGetValue(ArgumentNames.Authenticated, out var value)
            && value is BooleanValueNode { Value: true };

        return new MergedPolicy(
            authenticated,
            ReadRequirement(directive, ArgumentNames.Scopes),
            ReadRequirement(directive, ArgumentNames.Policies));
    }

    private static PolicyRequirement ReadRequirement(IDirective directive, string argumentName)
    {
        return directive.Arguments.ContainsName(argumentName)
            ? PolicyRequirement.Create(ParseGroups(directive, argumentName))
            : PolicyRequirement.Empty;
    }

    private static ImmutableArray<ImmutableArray<string>> ParseGroups(
        IDirective directive,
        string argumentName)
    {
        if (!directive.Arguments.TryGetValue(argumentName, out var argument))
        {
            throw ThrowHelper.PolicyDirectiveArgumentInvalid(directive.Name, argumentName);
        }

        // A single string coerces to a list of one group of one.
        if (argument is StringValueNode singleValue)
        {
            return [[singleValue.Value]];
        }

        if (argument is not ListValueNode outer)
        {
            throw ThrowHelper.PolicyDirectiveArgumentInvalid(directive.Name, argumentName);
        }

        var groups = ImmutableArray.CreateBuilder<ImmutableArray<string>>(outer.Items.Count);

        foreach (var item in outer.Items)
        {
            switch (item)
            {
                // A single string coerces to a group of one.
                case StringValueNode single:
                    groups.Add([single.Value]);
                    break;

                case ListValueNode inner:
                    var values = ImmutableArray.CreateBuilder<string>(inner.Items.Count);

                    foreach (var value in inner.Items)
                    {
                        if (value is not StringValueNode stringValue)
                        {
                            throw ThrowHelper.PolicyDirectiveArgumentInvalid(
                                directive.Name,
                                argumentName);
                        }

                        values.Add(stringValue.Value);
                    }

                    groups.Add(values.ToImmutable());
                    break;

                default:
                    throw ThrowHelper.PolicyDirectiveArgumentInvalid(
                        directive.Name,
                        argumentName);
            }
        }

        return groups.ToImmutable();
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
