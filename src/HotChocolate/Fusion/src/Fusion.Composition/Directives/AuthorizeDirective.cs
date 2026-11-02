using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Directives;

internal sealed class AuthorizeDirective(
    string? policy,
    List<string>? roles,
    ApplyPolicy? apply = null)
{
    public string? Policy { get; } = policy;

    public List<string>? Roles { get; } = roles;

    public ApplyPolicy? Apply { get; } = apply;

    public static AuthorizeDirective From(IDirective directive)
    {
        string? policy = null;
        List<string>? roles = null;
        ApplyPolicy? applyPolicy = null;

        if (directive.Arguments.TryGetValue(ArgumentNames.Policy, out var policyArg))
        {
            policy = policyArg switch
            {
                StringValueNode stringValueNode => stringValueNode.Value,
                NullValueNode => null,
                _ => throw new InvalidOperationException(AuthorizeDirective_PolicyArgument_Invalid)
            };
        }

        if (directive.Arguments.TryGetValue(ArgumentNames.Roles, out var rolesArg))
        {
            roles = rolesArg switch
            {
                ListValueNode listValueNode when listValueNode.Items.All(v => v is StringValueNode)
                    => listValueNode.Items.Cast<StringValueNode>().Select(v => v.Value).ToList(),
                NullValueNode => null,
                _ => throw new InvalidOperationException(AuthorizeDirective_RolesArgument_Invalid)
            };
        }

        if (directive.Arguments.TryGetValue(ArgumentNames.Apply, out var applyArg))
        {
            applyPolicy = applyArg switch
            {
                EnumValueNode enumValueNode => GetApplyPolicy(enumValueNode.Value),
                _ => throw new InvalidOperationException(AuthorizeDirective_ApplyArgument_Invalid)
            };
        }

        return new AuthorizeDirective(policy, roles, applyPolicy);
    }

    private static ApplyPolicy GetApplyPolicy(string applyPolicyValue)
    {
        return applyPolicyValue switch
        {
            "BEFORE_RESOLVER" => ApplyPolicy.BeforeResolver,
            "AFTER_RESOLVER" => ApplyPolicy.AfterResolver,
            "VALIDATION" => ApplyPolicy.Validation,
            _ => throw new InvalidOperationException(
                string.Format(AuthorizeDirective_ApplyArgument_InvalidEnumValue, applyPolicyValue))
        };
    }
}
