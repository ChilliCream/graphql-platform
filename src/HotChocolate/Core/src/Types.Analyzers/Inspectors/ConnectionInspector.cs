using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class ConnectionInspector : ClassWithBaseClassInspector<ConnectionClassInfo>
{
    protected override bool TryHandle(
        GeneratorSyntaxContext context,
        ClassDeclarationSyntax classDeclaration,
        SeparatedSyntaxList<BaseTypeSyntax> baseTypes,
        INamedTypeSymbol namedType,
        [NotNullWhen(true)] out ConnectionClassInfo? syntaxInfo)
    {
        if (namedType is { IsStatic: false, IsAbstract: false })
        {
            if (context.IsConnectionType(namedType))
            {
                syntaxInfo = ConnectionClassInfo.CreateConnection(
                    context.SemanticModel.Compilation,
                    namedType,
                    classDeclaration);
                return true;
            }

            if (context.IsEdgeType(namedType))
            {
                syntaxInfo = ConnectionClassInfo.CreateEdge(
                    context.SemanticModel.Compilation,
                    namedType,
                    classDeclaration);
                return true;
            }
        }

        syntaxInfo = null;
        return false;
    }
}
