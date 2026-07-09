using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    /// Extracts an equatable <see cref="LocationInfo"/> from a Roslyn <see cref="Location"/>,
    /// preserving Roslyn's 0-based line and column values for diagnostic reconstruction.
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

    /// <summary>
    /// Extracts an equatable <see cref="LocationInfo"/> from a Roslyn <see cref="Location"/> for
    /// declaration metadata, producing 1-based line and column values matching editor coordinates.
    /// </summary>
    /// <param name="location">The Roslyn location to convert.</param>
    /// <returns>
    /// A <see cref="LocationInfo"/> if the location is in source; otherwise, <see langword="null"/>.
    /// </returns>
    public static LocationInfo? ToDeclarationLocationInfo(this Location location)
    {
        if (!location.IsInSource)
        {
            return null;
        }

        var span = location.GetLineSpan();

        return new LocationInfo(
            span.Path,
            span.StartLinePosition.Line + 1,
            span.StartLinePosition.Character + 1,
            span.EndLinePosition.Line + 1,
            span.EndLinePosition.Character + 1);
    }

    /// <summary>
    /// Extracts the source location for an entire type declaration, producing 1-based line and
    /// column values matching editor coordinates.
    /// </summary>
    public static LocationInfo? ToDeclarationLocationInfo(this TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.GetLocation().ToDeclarationLocationInfo();
    }

    /// <summary>
    /// Extracts XML documentation for a source declaration.
    /// </summary>
    public static string? GetXmlDocumentation(
        this INamedTypeSymbol type,
        CancellationToken cancellationToken)
    {
        var xml = type.GetDocumentationCommentXml(
            preferredCulture: null,
            expandIncludes: false,
            cancellationToken: cancellationToken);

        return string.IsNullOrWhiteSpace(xml) ? null : xml;
    }

    /// <summary>
    /// Resolves the declaration location for a type symbol from its declaring syntax, choosing the
    /// ordinally smallest location when the type is declared across multiple partial parts.
    /// </summary>
    /// <returns>
    /// A <see cref="LocationInfo"/> for a source declaration; <see langword="null"/> for a
    /// metadata-only type that has no source declaration in the current compilation.
    /// </returns>
    public static LocationInfo? GetDeclarationLocationInfo(
        this INamedTypeSymbol type,
        CancellationToken cancellationToken)
    {
        // DeclaringSyntaxReferences has no guaranteed order across compilations or machines, so a
        // partial type spread across parts must collapse to one stable location. Imposing a total order
        // (path, then start line, then start column) and always taking the minimum keeps the emitted
        // DeclarationLocation byte-stable, which reproducible builds and snapshot tests depend on.
        // Smallest is an arbitrary but deterministic canonical choice; any consistent rule would do.
        LocationInfo? smallest = null;

        foreach (var syntaxReference in type.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is TypeDeclarationSyntax typeDeclaration)
            {
                smallest = LocationInfo.Min(smallest, typeDeclaration.ToDeclarationLocationInfo());
            }
        }

        return smallest;
    }

    /// <summary>
    /// Captures equatable declaration metadata (fully qualified name, XML documentation, and location)
    /// for a referenced type from its resolved symbol.
    /// </summary>
    /// <returns>
    /// A <see cref="DeclaredTypeInfo"/> for a source declaration; <see langword="null"/> for a
    /// metadata-only type that has no source declaration in the current compilation, or a non-named
    /// type that cannot carry source declaration metadata.
    /// </returns>
    public static DeclaredTypeInfo? ToDeclaredTypeInfo(
        this ITypeSymbol? type,
        CancellationToken cancellationToken)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return null;
        }

        var location = namedType.GetDeclarationLocationInfo(cancellationToken);

        if (location is null)
        {
            return null;
        }

        return new DeclaredTypeInfo(
            namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            namedType.GetXmlDocumentation(cancellationToken),
            location);
    }
}
