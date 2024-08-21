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
    public IReadOnlyList<ISyntaxFilter> Filters => [MethodWithAttribute.Instance];

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

                    if (fullName.Equals(WellKnownAttributes.DataLoaderAttribute, Ordinal) &&
                        context.SemanticModel.GetDeclaredSymbol(methodSyntax) is { } methodSymbol)
                    {
                        syntaxInfo = new DataLoaderInfo(
                            attributeSyntax,
                            attributeSymbol,
                            methodSymbol,
                            methodSyntax);
                        return true;
                    }
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
