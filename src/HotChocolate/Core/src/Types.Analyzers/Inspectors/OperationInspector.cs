using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class OperationInspector : ISyntaxInspector
{
    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out ISyntaxInfo? syntaxInfo)
    {
        if (context.Node is MethodDeclarationSyntax { AttributeLists.Count: > 0, } methodSyntax)
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

                    if (!fullName.Equals(WellKnownAttributes.QueryAttribute, StringComparison.Ordinal) ||
                        context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
                    {
                        continue;
                    }
                    
                    syntaxInfo = new DataLoaderInfo(
                        attributeSyntax,
                        attributeSymbol,
                        methodSymbol,
                        methodSyntax);
                    return true;
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}