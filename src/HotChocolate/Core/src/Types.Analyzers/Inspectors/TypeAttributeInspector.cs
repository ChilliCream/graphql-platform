using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class TypeAttributeInspector : ISyntaxInspector
{
    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out ISyntaxInfo? syntaxInfo)
    {
        if (context.Node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0 } possibleType)
        {
            foreach (AttributeListSyntax attributeListSyntax in possibleType.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

                    if (symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (WellKnownAttributes.ExtendObjectTypeAttribute.Equals(fullName, Ordinal) &&
                        context.SemanticModel.GetDeclaredSymbol(possibleType) is { } typeExt)
                    {
                        syntaxInfo = new TypeExtensionInfo(typeExt.ToDisplayString());
                        return true;
                    }

                    if (WellKnownAttributes.TypeAttributes.Contains(fullName) &&
                        context.SemanticModel.GetDeclaredSymbol(possibleType) is { } type)
                    {
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
