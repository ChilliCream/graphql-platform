using Microsoft.CodeAnalysis;

namespace Mocha.Analyzers.Utils;

/// <summary>
/// Provides extension methods for Roslyn <see cref="INamedTypeSymbol"/> to simplify
/// type hierarchy and interface implementation queries.
/// </summary>
public static class RoslynExtensions
{
    /// <summary>
    /// Determines whether the specified type symbol directly implements the given interface,
    /// comparing by original definition to match open generic interfaces.
    /// Only checks directly declared interfaces to avoid the expensive AllInterfaces walk.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <param name="interfaceType">The interface type to search for.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="type"/> directly implements <paramref name="interfaceType"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool ImplementsInterface(this INamedTypeSymbol type, INamedTypeSymbol interfaceType)
    {
        foreach (var @interface in type.Interfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, interfaceType.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the closed constructed interface matching the specified open generic interface
    /// in the type's directly declared interfaces.
    /// Only checks directly declared interfaces to avoid the expensive AllInterfaces walk.
    /// </summary>
    /// <param name="type">The type symbol to search.</param>
    /// <param name="openGenericInterface">The open generic interface definition to match against.</param>
    /// <returns>
    /// The closed constructed <see cref="INamedTypeSymbol"/> if found; otherwise, <see langword="null"/>.
    /// </returns>
    public static INamedTypeSymbol? FindImplementedInterface(
        this INamedTypeSymbol type,
        INamedTypeSymbol openGenericInterface)
    {
        foreach (var @interface in type.Interfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, openGenericInterface))
            {
                return @interface;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts an equatable <see cref="LocationInfo"/> from a Roslyn <see cref="Location"/>.
    /// </summary>
    /// <param name="location">The Roslyn location to convert.</param>
    /// <returns>
    /// A <see cref="LocationInfo"/> if the location is in source; otherwise, <see langword="null"/>.
    /// </returns>
    public static LocationInfo? ToLocationInfo(this Location location)
    {
        if (!location.IsInSource)
        {
            return null;
        }

        var span = location.GetLineSpan();

        return new LocationInfo(
            span.Path,
            span.StartLinePosition.Line,
            span.StartLinePosition.Character,
            span.EndLinePosition.Line,
            span.EndLinePosition.Character);
    }
}
