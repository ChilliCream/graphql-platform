using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mocha.Analyzers.Filters;

/// <summary>
/// Provides a singleton <see cref="ISyntaxFilter"/> that matches attribute list syntax nodes
/// (used to discover assembly-level attributes such as <c>[assembly: MediatorModule(...)]</c>).
/// </summary>
public sealed class AssemblyAttributeListFilter : ISyntaxFilter
{
    private AssemblyAttributeListFilter() { }

    /// <inheritdoc />
    public bool IsMatch(SyntaxNode node) => node is AttributeListSyntax { Target.Identifier.Text: "assembly" };

    /// <summary>
    /// Gets the singleton instance of <see cref="AssemblyAttributeListFilter"/>.
    /// </summary>
    public static AssemblyAttributeListFilter Instance { get; } = new();
}
