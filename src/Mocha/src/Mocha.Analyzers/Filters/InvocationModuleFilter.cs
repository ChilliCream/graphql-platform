using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mocha.Analyzers.Filters;

/// <summary>
/// Provides a singleton <see cref="ISyntaxFilter"/> that matches invocation expressions
/// where the method name starts with "Add". This is a cheap syntactic pre-screen for
/// module registration methods like <c>builder.AddOrderService()</c>.
/// </summary>
public sealed class InvocationModuleFilter : ISyntaxFilter
{
    private InvocationModuleFilter() { }

    /// <inheritdoc />
    public bool IsMatch(SyntaxNode node)
        => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess }
        && GetMethodName(memberAccess) is { } name
        && name.StartsWith("Add", StringComparison.Ordinal);

    /// <summary>
    /// Gets the singleton instance of <see cref="InvocationModuleFilter"/>.
    /// </summary>
    public static InvocationModuleFilter Instance { get; } = new();

    private static string? GetMethodName(MemberAccessExpressionSyntax memberAccess)
        => memberAccess.Name switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
}
