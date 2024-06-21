using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class OperationInspector : ISyntaxInspector
{
    public IReadOnlyList<ISyntaxFilter> Filters => [MethodWithAttribute.Instance];

    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
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
                    var operationType = ParseOperationType(fullName);

                    if(operationType == OperationType.No)
                    {
                        continue;
                    }

                    if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
                    {
                        continue;
                    }

                    if (!methodSymbol.IsStatic)
                    {
                        continue;
                    }

                    syntaxInfo = new OperationInfo(
                        operationType,
                        methodSymbol.ContainingType.ToDisplayString(),
                        methodSymbol.Name);
                    return true;
                }
            }
        }

        syntaxInfo = null;
        return false;
    }

    private OperationType ParseOperationType(string attributeName)
    {
        if (attributeName.Equals(WellKnownAttributes.QueryAttribute, StringComparison.Ordinal))
        {
            return OperationType.Query;
        }

        if (attributeName.Equals(WellKnownAttributes.MutationAttribute, StringComparison.Ordinal))
        {
            return OperationType.Mutation;
        }

        if (attributeName.Equals(WellKnownAttributes.SubscriptionAttribute, StringComparison.Ordinal))
        {
            return OperationType.Subscription;
        }

        return OperationType.No;
    }
}
