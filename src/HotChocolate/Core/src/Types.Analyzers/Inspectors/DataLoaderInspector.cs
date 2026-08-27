using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class DataLoaderInspector : ISyntaxInspector
{
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [MethodWithAttribute.Instance];

    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = [SyntaxKind.MethodDeclaration];

    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodSyntax)
        {
            foreach (var attributeListSyntax in methodSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

                    if (symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
                    {
                        continue;
                    }

                    if (fullName.Equals(WellKnownAttributes.DataLoaderAttribute, Ordinal))
                    {
                        var attributeData = methodSymbol.GetDataLoaderAttribute(attributeContainingTypeSymbol);

                        syntaxInfo = new DataLoaderInfo(
                            attributeSyntax,
                            attributeSymbol,
                            attributeData,
                            methodSymbol,
                            methodSyntax);
                        return true;
                    }

                    if (fullName.StartsWith(WellKnownAttributes.DataLoaderAttribute, Ordinal)
                        && attributeContainingTypeSymbol.TypeArguments.Length == 1
                        && GenericDataLoaderInfo.TryCreate(
                            attributeSyntax,
                            attributeSymbol,
                            methodSymbol.GetDataLoaderAttribute(attributeContainingTypeSymbol),
                            methodSymbol,
                            methodSyntax,
                            out var genericDataLoader))
                    {
                        syntaxInfo = genericDataLoader!;
                        return true;
                    }
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
