using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Filters;

public sealed class AssemblyAttributeList : ISyntaxFilter
{
    private AssemblyAttributeList() { }

    public static AssemblyAttributeList Instance { get; } = new();

    public bool IsMatch(SyntaxNode node)
        => node is AttributeListSyntax;
}
