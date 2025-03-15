using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Filters;

internal sealed class AddNodeIdValueSerializerFromMethod : ISyntaxFilter
{
    private AddNodeIdValueSerializerFromMethod() { }

    public static AddNodeIdValueSerializerFromMethod Instance { get; } = new();

    public bool IsMatch(SyntaxNode node)
        => node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "AddNodeIdValueSerializerFrom",
                },
            };
}
