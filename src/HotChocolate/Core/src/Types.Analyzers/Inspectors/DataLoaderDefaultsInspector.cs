using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;
using static HotChocolate.Types.Analyzers.WellKnownTypes;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class DataLoaderDefaultsInspector : ISyntaxInspector
{
    public IReadOnlyList<ISyntaxFilter> Filters => [AssemblyAttributeList.Instance];

    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is AttributeListSyntax attributeList)
        {
            foreach (var attributeSyntax in attributeList.Attributes)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;

                if (symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName.Equals(DataLoaderDefaultsAttribute, Ordinal) &&
                    attributeSyntax.ArgumentList is { Arguments.Count: > 0, } attribList)
                {
                    syntaxInfo = new DataLoaderDefaultsInfo(
                        attribList.Arguments.IsScoped(context),
                        attribList.Arguments.IsPublic(context),
                        attribList.Arguments.IsInterfacePublic(context),
                        attribList.Arguments.RegisterService(context),
                        attribList.Arguments.GenerateInterfaces(context));
                    return true;
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
