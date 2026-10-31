using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class AuthorizationDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value.Equals(FusionBuiltIns.Authorization, StringComparison.Ordinal);

    public static AuthorizationDirective? Parse(IReadOnlyList<DirectiveNode> directiveNodes)
    {
        for (var i = 0; i < directiveNodes.Count; i++)
        {
            if (CanParse(directiveNodes[i]))
            {
                return Parse(directiveNodes[i]);
            }
        }

        return null;
    }

    public static AuthorizationDirective Parse(DirectiveNode directiveNode)
    {
        var authenticated = false;
        ImmutableArray<ImmutableArray<string>> scopes = [];
        ImmutableArray<ImmutableArray<string>> policies = [];

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "authenticated":
                    authenticated = argument.Value is BooleanValueNode { Value: true };
                    break;

                case "scopes":
                    scopes = ParseGroups(argument);
                    break;

                case "policies":
                    policies = ParseGroups(argument);
                    break;

                default:
                    throw ThrowHelper.AuthorizationDirectiveArgumentNotSupported(argument.Name.Value);
            }
        }

        return new AuthorizationDirective(authenticated, scopes, policies);
    }

    private static ImmutableArray<ImmutableArray<string>> ParseGroups(ArgumentNode argument)
    {
        if (argument.Value is NullValueNode)
        {
            return [];
        }

        if (argument.Value is not ListValueNode outer)
        {
            throw ThrowHelper.AuthorizationDirectiveGroupsInvalid(argument.Name.Value);
        }

        var groups = ImmutableArray.CreateBuilder<ImmutableArray<string>>(outer.Items.Count);

        foreach (var item in outer.Items)
        {
            if (item is not ListValueNode inner)
            {
                throw ThrowHelper.AuthorizationDirectiveGroupsInvalid(argument.Name.Value);
            }

            var values = ImmutableArray.CreateBuilder<string>(inner.Items.Count);

            foreach (var value in inner.Items)
            {
                if (value is not StringValueNode stringValue)
                {
                    throw ThrowHelper.AuthorizationDirectiveGroupsInvalid(argument.Name.Value);
                }

                values.Add(stringValue.Value);
            }

            groups.Add(values.ToImmutable());
        }

        return groups.ToImmutable();
    }
}
