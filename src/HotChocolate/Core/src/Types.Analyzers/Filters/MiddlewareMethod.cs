using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Filters;

internal sealed class MiddlewareMethod : ISyntaxFilter
{
    private MiddlewareMethod() { }

    public static MiddlewareMethod Instance { get; } = new();

    public bool IsMatch(SyntaxNode node)
        => node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: var method,
                },
            } &&
            (method.Equals("UseRequest") || method.Equals("UseField") || method.Equals("Use"));
}
