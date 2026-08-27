using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var compilation = context.SemanticModel.Compilation;
            var dataLoaderAttribute = compilation.GetTypeByMetadataName(
                WellKnownAttributes.DataLoaderAttribute);
            var genericDataLoaderAttribute = compilation.GetTypeByMetadataName(
                WellKnownAttributes.GenericDataLoaderAttribute);

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

                    if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
                    {
                        continue;
                    }

                    if (dataLoaderAttribute is not null
                        && SymbolEqualityComparer.Default.Equals(
                            attributeContainingTypeSymbol.OriginalDefinition,
                            dataLoaderAttribute))
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

                    if (genericDataLoaderAttribute is not null
                        && SymbolEqualityComparer.Default.Equals(
                            attributeContainingTypeSymbol.OriginalDefinition,
                            genericDataLoaderAttribute)
                        && GenericDataLoaderInfo.TryCreate(
                            attributeSyntax,
                            attributeSymbol,
                            methodSymbol.GetDataLoaderAttribute(attributeContainingTypeSymbol),
                            methodSymbol,
                            methodSyntax,
                            compilation,
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
