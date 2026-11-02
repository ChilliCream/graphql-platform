using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

internal sealed class NodeIdValueSerializerInspector : ISyntaxInspector
{
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [AddNodeIdValueSerializerFromMethod.Instance];

    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = [SyntaxKind.InvocationExpression];

    public bool TryHandle(GeneratorSyntaxContext context, [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.ValueText: "AddNodeIdValueSerializerFrom",
                        TypeArgumentList: { Arguments.Count: 1 } args
                    }
                } memberAccess
            })
        {
            var semanticModel = context.SemanticModel;
            var idType = ModelExtensions.GetTypeInfo(semanticModel, args.Arguments[0]).Type;

            if (idType is not INamedTypeSymbol type)
            {
                syntaxInfo = default;
                return false;
            }

            var location = GetLocation(memberAccess, semanticModel);
            syntaxInfo = new NodeIdValueSerializerInfo(type, location);
            return true;
        }

        syntaxInfo = default;
        return false;
    }

    private static (string, int, int) GetLocation(
        MemberAccessExpressionSyntax memberAccessExpression,
        SemanticModel semanticModel)
    {
        var invocationNameSpan = memberAccessExpression.Name.Span;
        var lineSpan = memberAccessExpression.SyntaxTree.GetLineSpan(invocationNameSpan);
        var filePath = GetInterceptorFilePath(
            memberAccessExpression.SyntaxTree,
            semanticModel.Compilation.Options.SourceReferenceResolver);
        return (filePath, lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1);
    }

    private static string GetInterceptorFilePath(SyntaxTree tree, SourceReferenceResolver? resolver)
        => resolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
}
