using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Represents an inspector that detects abstract class or record declarations implementing any handler interface
/// and reports the <c>MO0003</c> diagnostic to warn that abstract handlers cannot be registered.
/// </summary>
public sealed class AbstractHandlerInspector : ISyntaxInspector
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
        if (node is not TypeDeclarationSyntax typeDeclaration)
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

        // Only handle abstract types (non-interface)
        if (!namedTypeSymbol.IsAbstract || namedTypeSymbol.TypeKind == TypeKind.Interface)
        {
            syntaxInfo = null;
            return false;
        }

        // Check if it implements any handler interface
        if (!ImplementsAnyHandlerInterface(knownSymbols, namedTypeSymbol))
        {
            syntaxInfo = null;
            return false;
        }

        var handlerName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var locationInfo = typeDeclaration.Identifier.GetLocation().ToLocationInfo();

        // Create a placeholder SyntaxInfo carrying the diagnostic
        syntaxInfo = new AbstractHandlerDiagnosticInfo(handlerName)
        {
            Diagnostics = new([
                new DiagnosticInfo(Errors.AbstractHandler.Id, locationInfo, new([handlerName]))
            ])
        };
        return true;
    }

    private static bool ImplementsAnyHandlerInterface(KnownTypeSymbols knownSymbols, INamedTypeSymbol namedTypeSymbol)
    {
        if (knownSymbols.ICommandHandlerVoid is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.ICommandHandlerVoid) is not null)
        {
            return true;
        }

        if (knownSymbols.ICommandHandlerResponse is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.ICommandHandlerResponse) is not null)
        {
            return true;
        }

        if (knownSymbols.IQueryHandler is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.IQueryHandler) is not null)
        {
            return true;
        }

        if (knownSymbols.INotificationHandler is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.INotificationHandler) is not null)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// A diagnostic-only SyntaxInfo used to carry MO0003 diagnostics.
/// This is not used by code generators.
/// </summary>
internal sealed record AbstractHandlerDiagnosticInfo(string HandlerTypeName) : SyntaxInfo
{
    public override string OrderByKey => $"AbstractDiag:{HandlerTypeName}";
}
