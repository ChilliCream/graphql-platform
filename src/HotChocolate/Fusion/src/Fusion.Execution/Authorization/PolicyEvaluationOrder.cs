using HotChocolate.Types;

namespace HotChocolate.Fusion.Authorization;

internal static class PolicyEvaluationOrder
{
    /// <summary>
    /// Gets the position of the directive in the evaluation order: authenticated,
    /// then scopes, then named policies.
    /// </summary>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    public static int Get(string directiveName)
    {
        ArgumentNullException.ThrowIfNull(directiveName);

        return directiveName switch
        {
            DirectiveNames.Authenticated.Name => 0,
            DirectiveNames.RequiresScopes.Name => 1,
            _ => 2
        };
    }
}
