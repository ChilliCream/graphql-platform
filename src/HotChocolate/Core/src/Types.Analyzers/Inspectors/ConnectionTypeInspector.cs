using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class ConnectionTypeInspector : ISyntaxInspector
{
    public IReadOnlyList<ISyntaxFilter> Filters => [ClassWithBaseClass.Instance];

    public bool TryHandle(GeneratorSyntaxContext context, [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, TypeParameterList: null, } possibleType)
        {
            var typeSymbol = context.SemanticModel.GetDeclaredSymbol(possibleType);

            if (typeSymbol?.IsAbstract == false
                && typeSymbol.IsConnectionType(context.SemanticModel.Compilation))
            {
                syntaxInfo = new ConnectionTypeInfo(typeSymbol, possibleType);
                return true;
            }
        }

        syntaxInfo = null;
        return false;
    }
}
