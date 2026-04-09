using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Inspects invocation expressions to discover calls to source-generated module registration
/// methods (e.g. <c>builder.AddOrderService()</c>) and extracts their declared message types.
/// </summary>
/// <remarks>
/// <para>
/// The source generator decorates each generated <c>Add*</c> extension method with a
/// <c>[MessagingModuleInfo(MessageTypes = new[] { typeof(A), typeof(B) })]</c> attribute
/// that lists every message type the module handles. This inspector resolves the called
/// method symbol, reads that attribute, and emits an <see cref="ImportedModuleTypesInfo"/>
/// containing the type names.
/// </para>
/// <para>
/// Downstream generators use this to avoid emitting duplicate serializer registrations for
/// types already covered by a referenced module.
/// </para>
/// <para>
/// The syntactic pre-filter (<see cref="InvocationModuleFilter"/>) broadly matches any
/// method starting with <c>Add</c>; this inspector then narrows to only those carrying
/// the <c>[MessagingModuleInfo]</c> attribute.
/// </para>
/// </remarks>
public sealed class ImportedModuleTypeInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [InvocationModuleFilter.Instance];

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
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        // Look for [MessagingModuleInfo] on the resolved method.
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != SyntaxConstants.MessagingModuleInfoAttribute)
            {
                continue;
            }

            // Extract the MessageTypes named argument: MessageTypes = new Type[] { typeof(A), typeof(B) }
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key != SyntaxConstants.MessageTypesProperty)
                {
                    continue;
                }

                if (namedArg.Value.Kind != TypedConstantKind.Array)
                {
                    continue;
                }

                var typeNames = new List<string>();

                foreach (var element in namedArg.Value.Values)
                {
                    if (element.Value is INamedTypeSymbol typeSymbol)
                    {
                        typeNames.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    }
                }

                if (typeNames.Count > 0)
                {
                    syntaxInfo = new ImportedModuleTypesInfo(
                        methodSymbol.Name,
                        new ImmutableEquatableArray<string>(typeNames));
                    return true;
                }
            }
        }

        return false;
    }
}
