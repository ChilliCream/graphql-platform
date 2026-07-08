using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Inspects <c>AddMessage&lt;TMessage&gt;()</c> invocations on the Mocha builder APIs
/// (<c>MessageBusHostBuilderExtensions</c> and <c>IMessageBusBuilder</c>) and captures declaration metadata
/// for the registered message type.
/// </summary>
/// <remarks>
/// This is a metadata-only discovery source. It produces an <see cref="AddMessageDeclarationInfo"/> that feeds
/// the message declaration pipeline only; it never participates in AOT JsonSerializerContext validation or code
/// emission, which is why it is a distinct record from <see cref="CallSiteMessageTypeInfo"/>.
/// </remarks>
public sealed class AddMessageDeclarationInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [AddMessageCallSiteFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = ImmutableHashSet.Create(SyntaxKind.InvocationExpression);

    /// <inheritdoc />
    public bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        syntaxInfo = null;

        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol { Name: "AddMessage" } methodSymbol)
        {
            return false;
        }

        var containingType = methodSymbol.ContainingType?.OriginalDefinition;
        if (containingType is null || !IsMochaBuilderApi(knownSymbols, containingType))
        {
            return false;
        }

        // AddMessage<TMessage>() carries the message type as its single type argument.
        if (methodSymbol.TypeArguments.Length != 1
            || methodSymbol.TypeArguments[0] is not INamedTypeSymbol messageType)
        {
            return false;
        }

        // Capture declaration metadata (doc + span) cross-file from the resolved symbol. Metadata-only types
        // (declared in another assembly) carry no source declaration, so there is nothing to contribute. This
        // is stale only in the IDE's incremental cache; every real compiler invocation runs fresh.
        var declaredMessageType = messageType.ToDeclaredTypeInfo(cancellationToken);
        if (declaredMessageType is null)
        {
            return false;
        }

        syntaxInfo = new AddMessageDeclarationInfo(declaredMessageType);
        return true;
    }

    private static bool IsMochaBuilderApi(KnownTypeSymbols knownSymbols, INamedTypeSymbol containingType)
        => (knownSymbols.MessageBusHostBuilderExtensions is not null
            && SymbolEqualityComparer.Default.Equals(containingType, knownSymbols.MessageBusHostBuilderExtensions))
        || (knownSymbols.IMessageBusBuilder is not null
            && SymbolEqualityComparer.Default.Equals(containingType, knownSymbols.IMessageBusBuilder));
}
