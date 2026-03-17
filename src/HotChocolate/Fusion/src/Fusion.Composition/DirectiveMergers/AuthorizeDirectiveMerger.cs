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

internal class AuthorizeDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.Authorize;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(MutableSchemaDefinition schema)
    {
        return AuthorizeMutableDirectiveDefinition.Create(schema);
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

        var uniqueAuthorizeDirectives =
            memberDefinitions
                .SelectMany(d => d.Member.Directives.Where(dir => dir.Name == DirectiveNames.Authorize))
                .Select(AuthorizeDirective.From)
                .Distinct(AuthorizeDirectiveEqualityComparer.Instance);

        foreach (var authorizeDirective in uniqueAuthorizeDirectives)
        {
            var arguments = new List<ArgumentAssignment>();

            if (authorizeDirective.Policy is not null)
            {
                arguments.Add(
                    new ArgumentAssignment(ArgumentNames.Policy, authorizeDirective.Policy));
            }

            if (authorizeDirective.Roles is not null)
            {
                arguments.Add(
                    new ArgumentAssignment(
                        ArgumentNames.Roles,
                        new ListValueNode(
                            authorizeDirective.Roles
                                .Order()
                                .Select(r => new StringValueNode(r))
                                .ToList())));
            }

            if (authorizeDirective.Apply is not null)
            {
                arguments.Add(
                    new ArgumentAssignment(
                        ArgumentNames.Apply,
                        new EnumValueNode(authorizeDirective.Apply switch
                        {
                            ApplyPolicy.AfterResolver => "AFTER_RESOLVER",
                            ApplyPolicy.Validation => "VALIDATION",
                            _ => "BEFORE_RESOLVER"
                        })));
            }

            mergedMember.AddDirective(new Directive(directiveDefinition, arguments));
        }
    }

    private sealed class AuthorizeDirectiveEqualityComparer
        : IEqualityComparer<AuthorizeDirective>
    {
        public static readonly AuthorizeDirectiveEqualityComparer Instance = new();

        public bool Equals(AuthorizeDirective? x, AuthorizeDirective? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Policy == y.Policy
                && x.Apply == y.Apply
                && RolesEqual(x.Roles, y.Roles);
        }

        public int GetHashCode(AuthorizeDirective obj)
        {
            var hash = new HashCode();
            hash.Add(obj.Policy);
            hash.Add(obj.Apply);

            if (obj.Roles is not null)
            {
                foreach (var role in obj.Roles.Order())
                {
                    hash.Add(role);
                }
            }

            return hash.ToHashCode();
        }

        private static bool RolesEqual(List<string>? a, List<string>? b)
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            if (a.Count != b.Count)
            {
                return false;
            }

            return a.Order().SequenceEqual(b.Order());
        }
    }
}
