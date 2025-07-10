using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public abstract class ClassWithBaseClassInspector<T> : ISyntaxInspector where T : SyntaxInfo
{
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [ClassWithBaseClass.Instance];

    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = ImmutableHashSet.Create(SyntaxKind.ClassDeclaration);

    public bool TryHandle(GeneratorSyntaxContext context, [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } classDeclaration
            && context.SemanticModel.GetDeclaredSymbol(classDeclaration) is { } namedType
            && TryHandle(context, classDeclaration, classDeclaration.BaseList.Types, namedType, out var result))
        {
            syntaxInfo = result;
            return true;
        }

        syntaxInfo = null;
        return false;
    }

    protected abstract bool TryHandle(
        GeneratorSyntaxContext context,
        ClassDeclarationSyntax classDeclaration,
        SeparatedSyntaxList<BaseTypeSyntax> baseTypes,
        INamedTypeSymbol namedType,
        [NotNullWhen(true)] out T? syntaxInfo);
}
