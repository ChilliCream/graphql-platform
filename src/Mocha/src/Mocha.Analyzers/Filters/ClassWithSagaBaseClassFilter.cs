using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mocha.Analyzers.Filters;

/// <summary>
/// A singleton <see cref="ISyntaxFilter"/> that matches type declarations whose base list contains
/// a type name starting with "Saga". This narrows the predicate to avoid flooding the transform
/// phase with irrelevant types.
/// </summary>
public sealed class ClassWithSagaBaseClassFilter : ISyntaxFilter
{
    private ClassWithSagaBaseClassFilter() { }

    /// <inheritdoc />
    public bool IsMatch(SyntaxNode node)
        => node is TypeDeclarationSyntax { BaseList.Types.Count: > 0 } typeDecl
            && HasSagaBaseType(typeDecl.BaseList);

    /// <summary>
    /// Gets the singleton instance of <see cref="ClassWithSagaBaseClassFilter"/>.
    /// </summary>
    public static ClassWithSagaBaseClassFilter Instance { get; } = new();

    private static bool HasSagaBaseType(BaseListSyntax baseList)
    {
        foreach (var baseType in baseList.Types)
        {
            var name = GetBaseTypeName(baseType);
            if (name?.StartsWith("Saga", StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetBaseTypeName(BaseTypeSyntax baseType)
        => baseType.Type switch
        {
            SimpleNameSyntax simple => simple.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
            _ => null
        };
}
