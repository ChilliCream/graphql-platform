using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mocha.Analyzers.Filters;

/// <summary>
/// Provides a singleton <see cref="ISyntaxFilter"/> that matches invocation expressions whose member-access
/// method name is <c>AddMessage</c>. This is a cheap syntactic pre-screen for explicit message registrations
/// (<c>builder.AddMessage&lt;T&gt;()</c>); the inspector then narrows to the Mocha builder APIs semantically.
/// </summary>
public sealed class AddMessageCallSiteFilter : ISyntaxFilter
{
    private AddMessageCallSiteFilter() { }

    /// <inheritdoc />
    public bool IsMatch(SyntaxNode node)
        => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess }
        && GetMethodName(memberAccess) == "AddMessage";

    /// <summary>
    /// Gets the singleton instance of <see cref="AddMessageCallSiteFilter"/>.
    /// </summary>
    public static AddMessageCallSiteFilter Instance { get; } = new();

    private static string? GetMethodName(MemberAccessExpressionSyntax memberAccess)
        => memberAccess.Name switch
        {
            GenericNameSyntax generic => generic.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
}
