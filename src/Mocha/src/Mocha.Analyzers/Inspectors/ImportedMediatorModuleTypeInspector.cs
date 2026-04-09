using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Inspects invocation expressions to discover calls to source-generated mediator module
/// registration methods (e.g. <c>builder.AddOrderService()</c>) and extracts their declared
/// message and handler types.
/// </summary>
/// <remarks>
/// <para>
/// The source generator decorates each generated <c>Add*</c> extension method with a
/// <c>[MediatorModuleInfo(MessageTypes = new[] { typeof(A) }, HandlerTypes = new[] { typeof(B) })]</c>
/// attribute that lists every message and handler type the module registers. This inspector
/// resolves the called method symbol, reads that attribute, and emits an
/// <see cref="ImportedMediatorModuleTypesInfo"/> containing the type names.
/// </para>
/// <para>
/// Downstream validators use this to avoid reporting false-positive MO0001 and MO0020
/// diagnostics for types whose handlers exist in a referenced module.
/// </para>
/// <para>
/// The syntactic pre-filter (<see cref="InvocationModuleFilter"/>) broadly matches any
/// method starting with <c>Add</c>; this inspector then narrows to only those carrying
/// the <c>[MediatorModuleInfo]</c> attribute.
/// </para>
/// </remarks>
public sealed class ImportedMediatorModuleTypeInspector : ISyntaxInspector
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

        // Look for [MediatorModuleInfo] on the resolved method.
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != SyntaxConstants.MediatorModuleInfoAttribute)
            {
                continue;
            }

            var messageTypeNames = new List<string>();
            var handlerTypeNames = new List<string>();

            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Value.Kind != TypedConstantKind.Array)
                {
                    continue;
                }

                List<string>? targetList = namedArg.Key switch
                {
                    SyntaxConstants.MessageTypesProperty => messageTypeNames,
                    SyntaxConstants.HandlerTypesProperty => handlerTypeNames,
                    _ => null
                };

                if (targetList is null)
                {
                    continue;
                }

                foreach (var element in namedArg.Value.Values)
                {
                    if (element.Value is INamedTypeSymbol typeSymbol)
                    {
                        targetList.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    }
                }
            }

            if (messageTypeNames.Count > 0 || handlerTypeNames.Count > 0)
            {
                syntaxInfo = new ImportedMediatorModuleTypesInfo(
                    methodSymbol.Name,
                    new ImmutableEquatableArray<string>(messageTypeNames),
                    new ImmutableEquatableArray<string>(handlerTypeNames));
                return true;
            }
        }

        return false;
    }
}
