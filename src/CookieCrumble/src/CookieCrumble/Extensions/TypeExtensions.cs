namespace CookieCrumble;

/// <summary>
/// Some extensions for Type, to support snapshot testing.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Returns the list of inherited types.
    /// </summary>
    /// <param name="type">The current object type.</param>
    /// <returns>The list of all inherited types.</returns>
    public static IEnumerable<Type> BaseTypesAndSelf(this Type type)
    {
        var current = type;

        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
