using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mocha.Analyzers.Filters;

/// <summary>
/// Provides a singleton <see cref="ISyntaxFilter"/> that matches invocation expressions
/// where the method name is one of the known message dispatch methods. This is a cheap
/// syntactic check - no semantic analysis is performed.
/// </summary>
public sealed class InvocationCallSiteFilter : ISyntaxFilter
{
    private InvocationCallSiteFilter() { }

    /// <inheritdoc />
    public bool IsMatch(SyntaxNode node)
        => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess }
        && IsDispatchMethodName(GetMethodName(memberAccess));

    /// <summary>
    /// Gets the singleton instance of <see cref="InvocationCallSiteFilter"/>.
    /// </summary>
    public static InvocationCallSiteFilter Instance { get; } = new();

    private static string? GetMethodName(MemberAccessExpressionSyntax memberAccess)
        => memberAccess.Name switch
        {
            GenericNameSyntax generic => generic.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

    private static bool IsDispatchMethodName(string? name)
        => name
            is "SendAsync"
                or "PublishAsync"
                or "QueryAsync"
                or "ScheduleSendAsync"
                or "SchedulePublishAsync"
                or "RequestAsync";
}
