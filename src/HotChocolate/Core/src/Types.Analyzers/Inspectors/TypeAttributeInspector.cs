using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class TypeAttributeInspector : ISyntaxInspector
{
    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out ISyntaxInfo? syntaxInfo)
    {
        if (context.Node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0 } possibleType)
        {
            foreach (var attributeListSyntax in possibleType.AttributeLists)
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

                    // We do a start with here to capture the generic and non generic variant of
                    // the object type extension attribute.
                    if (fullName.StartsWith(ExtendObjectTypeAttribute, Ordinal) &&
                        context.SemanticModel.GetDeclaredSymbol(possibleType) is { } typeExt)
                    {
                        syntaxInfo = new TypeExtensionInfo(
                            typeExt.ToDisplayString(),
                            possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)));
                        return true;
                    }

                    if (TypeAttributes.Contains(fullName) &&
                        context.SemanticModel.GetDeclaredSymbol(possibleType) is { } type)
                    {
                        if (fullName.Equals(QueryTypeAttribute))
                        {
                            syntaxInfo = new TypeExtensionInfo(
                                type.ToDisplayString(),
                                possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)),
                                OperationType.Query);
                            return true;
                        }

                        if (fullName.Equals(MutationTypeAttribute))
                        {
                            syntaxInfo = new TypeExtensionInfo(
                                type.ToDisplayString(),
                                possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)),
                                OperationType.Mutation);
                            return true;
                        }

                        if (fullName.Equals(SubscriptionTypeAttribute))
                        {
                            syntaxInfo = new TypeExtensionInfo(
                                type.ToDisplayString(),
                                possibleType.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword)),
                                OperationType.Subscription);
                            return true;
                        }

                        syntaxInfo = new TypeInfo(type.ToDisplayString());
                        return true;
                    }
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
