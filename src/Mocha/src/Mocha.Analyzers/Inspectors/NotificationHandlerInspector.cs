using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Represents an inspector that detects concrete class or record declarations implementing
/// <c>INotificationHandler&lt;T&gt;</c>.
/// </summary>
public sealed class NotificationHandlerInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [ClassWithMochaBaseListFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } =
        ImmutableHashSet.Create(SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration);

    /// <inheritdoc />
    public bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        if (node is not TypeDeclarationSyntax typeDeclaration
            || knownSymbols.INotificationHandler is null)
        {
            syntaxInfo = null;
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

        if (typeSymbol is not { } namedTypeSymbol)
        {
            syntaxInfo = null;
            return false;
        }

        // Skip abstract types and interfaces
        if (namedTypeSymbol.IsAbstract || namedTypeSymbol.TypeKind == TypeKind.Interface)
        {
            syntaxInfo = null;
            return false;
        }

        var implemented = namedTypeSymbol.FindImplementedInterface(knownSymbols.INotificationHandler);
        if (implemented is null)
        {
            syntaxInfo = null;
            return false;
        }

        var notificationType = implemented.TypeArguments[0];

        syntaxInfo = new NotificationHandlerInfo(
            namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            namedTypeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            notificationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        return true;
    }
}
