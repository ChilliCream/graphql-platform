using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Detects abstract class declarations implementing messaging handler interfaces
/// and reports the <c>MO0013</c> diagnostic to warn that abstract handlers cannot be registered.
/// </summary>
public sealed class AbstractMessagingHandlerInspector : ISyntaxInspector
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

        // Check if it implements any messaging handler interface
        if (!ImplementsAnyMessagingHandlerInterface(knownSymbols, namedTypeSymbol))
        {
            syntaxInfo = null;
            return false;
        }

        var handlerName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var locationInfo = typeDeclaration.Identifier.GetLocation().ToLocationInfo();

        // Create a placeholder SyntaxInfo carrying the diagnostic
        syntaxInfo = new AbstractMessagingHandlerDiagnosticInfo(handlerName)
        {
            Diagnostics = new([
                new DiagnosticInfo(Errors.AbstractMessagingHandler.Id, locationInfo, new([handlerName]))
            ])
        };
        return true;
    }

    private static bool ImplementsAnyMessagingHandlerInterface(
        KnownTypeSymbols knownSymbols,
        INamedTypeSymbol namedTypeSymbol)
    {
        if (knownSymbols.IBatchEventHandler is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.IBatchEventHandler) is not null)
        {
            return true;
        }

        if (knownSymbols.IConsumer is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.IConsumer) is not null)
        {
            return true;
        }

        if (knownSymbols.IEventRequestHandlerResponse is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.IEventRequestHandlerResponse) is not null)
        {
            return true;
        }

        if (knownSymbols.IEventRequestHandlerVoid is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.IEventRequestHandlerVoid) is not null)
        {
            return true;
        }

        if (knownSymbols.IEventHandler is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.IEventHandler) is not null)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// A diagnostic-only SyntaxInfo for MO0013 (abstract messaging handler).
/// This is not used by code generators.
/// </summary>
internal sealed record AbstractMessagingHandlerDiagnosticInfo(string HandlerTypeName) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgAbstractDiag:{HandlerTypeName}";
}
