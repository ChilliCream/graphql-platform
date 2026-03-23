using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mocha.Analyzers.Filters;

/// <summary>
/// Provides a singleton <see cref="ISyntaxFilter"/> that matches type declarations with a base list
/// containing candidate Mocha interface names. This narrows the syntactic predicate to avoid
/// flooding the transform phase with irrelevant types.
/// </summary>
public sealed class ClassWithMochaBaseListFilter : ISyntaxFilter
{
    private ClassWithMochaBaseListFilter() { }

    /// <inheritdoc />
    public bool IsMatch(SyntaxNode node)
        => node is TypeDeclarationSyntax { BaseList.Types.Count: > 0 } typeDecl
            && HasCandidateBaseType(typeDecl.BaseList);

    /// <summary>
    /// Gets the singleton instance of <see cref="ClassWithMochaBaseListFilter"/>.
    /// </summary>
    public static ClassWithMochaBaseListFilter Instance { get; } = new();

    private static bool HasCandidateBaseType(BaseListSyntax baseList)
    {
        foreach (var baseType in baseList.Types)
        {
            var name = GetBaseTypeName(baseType);
            if (name is not null && IsMochaCandidateName(name))
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

    private static bool IsMochaCandidateName(string name)
        => name.StartsWith("ICommand", StringComparison.Ordinal)
        || name.StartsWith("IQuery", StringComparison.Ordinal)
        || name.StartsWith("INotification", StringComparison.Ordinal)
        || name.StartsWith("IStream", StringComparison.Ordinal);
}
