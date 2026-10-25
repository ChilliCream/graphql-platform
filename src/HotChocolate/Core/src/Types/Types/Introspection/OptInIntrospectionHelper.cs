namespace HotChocolate.Types.Introspection;

internal static class OptInIntrospectionHelper
{
    /// <summary>
    /// Determines whether a type system member is included for the given set of opted-in features.
    /// A member without any <c>@requiresOptIn</c> directives is always included. A member that
    /// requires one or more features is only included when <paramref name="includeOptIn"/> lists at
    /// least one of those features.
    /// </summary>
    /// <param name="directives">
    /// The directives of the type system member.
    /// </param>
    /// <param name="includeOptIn">
    /// The features the client opted into.
    /// </param>
    public static bool IsIncluded(IReadOnlyDirectiveCollection directives, string[] includeOptIn)
    {
        var requiresOptIn = false;

        foreach (var directive in directives)
        {
            if (directive.Definition is RequiresOptInDirectiveType)
            {
                requiresOptIn = true;

                if (includeOptIn.Contains(directive.ToValue<RequiresOptIn>().Feature))
                {
                    return true;
                }
            }
        }

        return !requiresOptIn;
    }
}
