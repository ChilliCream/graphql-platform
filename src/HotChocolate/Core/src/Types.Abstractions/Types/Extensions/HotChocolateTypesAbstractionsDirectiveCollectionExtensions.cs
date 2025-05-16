#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// Provides extension methods for <see cref="IReadOnlyDirectiveCollection"/>.
/// </summary>
public static class HotChocolateTypesAbstractionsDirectiveCollectionExtensions
{
    /// <summary>
    /// Returns the first directive that matches the specified runtime type.
    /// </summary>
    /// <typeparam name="T">The runtime type of the directive.</typeparam>
    /// <param name="directives">The collection of directives.</param>
    /// <returns>
    /// The first directive that matches the specified runtime type,
    /// or <see langword="null"/> if no such directive is found.
    /// </returns>
    public static IDirective? FirstOrDefault<T>(this IReadOnlyDirectiveCollection directives)
    {
        ArgumentNullException.ThrowIfNull(directives);

        return directives.FirstOrDefault(typeof(T));
    }

    /// <summary>
    /// Returns the value of the first directive that matches the specified runtime type.
    /// </summary>
    /// <typeparam name="T">The runtime type of the directive.</typeparam>
    /// <param name="directives">The collection of directives.</param>
    /// <returns>
    /// The value of the first directive that matches the specified runtime type,
    /// or <see langword="null"/> if no such directive is found.
    /// </returns>
    public static T? FirstOrDefaultValue<T>(this IReadOnlyDirectiveCollection directives)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(directives);

        var directive = FirstOrDefault<T>(directives);

        if (directive is null)
        {
            return default;
        }

        return directive.ToValue<T>();
    }
}
