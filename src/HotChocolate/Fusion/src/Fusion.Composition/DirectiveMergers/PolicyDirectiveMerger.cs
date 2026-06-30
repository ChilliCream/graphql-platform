using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
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

internal sealed class PolicyDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.Policy;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(MutableSchemaDefinition schema)
    {
        return PolicyMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<DirectivesProviderInfo> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        if (!mergedSchema.DirectiveDefinitions.TryGetDirective(DirectiveName, out var directiveDefinition))
        {
            return;
        }

        AddPolicyDirectives(
            mergedMember,
            MergePolicyDirectives(memberDefinitions),
            directiveDefinition);
    }

    public static List<PolicyDirective> MergePolicyDirectives(
        ImmutableArray<DirectivesProviderInfo> memberDefinitions)
    {
        return MergePolicyDirectives(
            memberDefinitions
                .SelectMany(d => d.Member.Directives.Where(dir => dir.Name == DirectiveNames.Policy))
                .Select(PolicyDirective.From));
    }

    public static List<PolicyDirective> MergePolicyDirectives(IEnumerable<PolicyDirective> policies)
    {
        return
        [
            .. policies
                .GroupBy(p => p.Name, StringComparer.Ordinal)
                .Select(g => new PolicyDirective(
                    g.Key,
                    g.MaxBy(p => GetOnDeniedRank(p.OnDenied))!.OnDenied))
        ];
    }

    public static void AddPolicyDirectives(
        IDirectivesProvider member,
        IReadOnlyList<PolicyDirective> policyDirectives,
        MutableDirectiveDefinition directiveDefinition)
    {
        foreach (var policyDirective in policyDirectives)
        {
            var arguments = new List<ArgumentAssignment>
            {
                new(ArgumentNames.Name, policyDirective.Name)
            };

            if (policyDirective.OnDenied != "NULL")
            {
                arguments.Add(
                    new ArgumentAssignment(
                        ArgumentNames.OnDenied,
                        new EnumValueNode(policyDirective.OnDenied)));
            }

            member.AddDirective(new Directive(directiveDefinition, arguments));
        }
    }

    private static int GetOnDeniedRank(string onDenied)
    {
        return onDenied switch
        {
            "NULL" => 0,
            "ERROR" => 1,
            "ABORT" => 2,
            _ => throw new InvalidOperationException(
                $"The value `{onDenied}` is not supported by @policy onDenied.")
        };
    }
}
