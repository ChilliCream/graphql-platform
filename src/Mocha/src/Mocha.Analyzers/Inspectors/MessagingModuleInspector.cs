using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Inspects assembly-level attribute lists to discover <c>[assembly: MessagingModule("...")]</c>
/// declarations and extract <see cref="MessagingModuleInfo"/> from them.
/// </summary>
public sealed class MessagingModuleInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [AssemblyAttributeListFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = [SyntaxKind.AttributeList];

    /// <inheritdoc />
    public bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        if (node is AttributeListSyntax attributeList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var attributeSyntax in attributeList.Attributes)
            {
                var symbol = ModelExtensions.GetSymbolInfo(semanticModel, attributeSyntax).Symbol;

                if (symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (string.Equals(fullName, SyntaxConstants.MessagingModuleAttribute, StringComparison.Ordinal)
                    && attributeSyntax.ArgumentList is { Arguments.Count: > 0 })
                {
                    var nameExpr = attributeSyntax.ArgumentList.Arguments[0].Expression;
                    var constantValue = semanticModel.GetConstantValue(nameExpr);

                    if (!constantValue.HasValue || constantValue.Value is not string name)
                    {
                        continue;
                    }

                    syntaxInfo = new MessagingModuleInfo(name);
                    return true;
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
