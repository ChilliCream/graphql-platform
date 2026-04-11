using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Inspects dispatch call sites (<c>SendAsync</c>, <c>PublishAsync</c>, <c>RequestAsync</c>,
/// <c>QueryAsync</c>, etc.) on <c>IMessageBus</c>, <c>ISender</c>, and <c>IPublisher</c>
/// to extract the compile-time message and response types being dispatched.
/// </summary>
/// <remarks>
/// <para>
/// The extracted types feed downstream generators and analyzers that verify serializer
/// registrations (MO0018) and handler existence (MO0020).
/// </para>
/// <para>
/// Type extraction varies by method shape: for most generic methods the message type comes
/// from the type argument <c>T</c>, but for <c>RequestAsync&lt;TResponse&gt;</c> the type
/// argument is the <em>response</em> type - the message type is inferred from the first
/// argument's compile-time type instead. Non-generic overloads (e.g.
/// <c>RequestAsync(object, …)</c>) also fall back to argument-expression analysis.
/// </para>
/// </remarks>
public sealed class CallSiteMessageTypeInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [InvocationCallSiteFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = ImmutableHashSet.Create(SyntaxKind.InvocationExpression);

    /// <inheritdoc />
    public bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        syntaxInfo = null;

        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        var receiverType = methodSymbol.ContainingType?.OriginalDefinition;
        if (receiverType is null)
        {
            return false;
        }

        // Try IMessageBus
        if (knownSymbols.IMessageBus is not null
            && SymbolEqualityComparer.Default.Equals(receiverType, knownSymbols.IMessageBus))
        {
            return TryHandleMessageBus(methodSymbol, invocation, semanticModel, cancellationToken, out syntaxInfo);
        }

        // Try ISender (Mediator)
        if (knownSymbols.ISender is not null
            && SymbolEqualityComparer.Default.Equals(receiverType, knownSymbols.ISender))
        {
            return TryHandleSender(methodSymbol, invocation, semanticModel, cancellationToken, out syntaxInfo);
        }

        // Try IPublisher (Mediator)
        if (knownSymbols.IPublisher is not null
            && SymbolEqualityComparer.Default.Equals(receiverType, knownSymbols.IPublisher))
        {
            return TryHandlePublisher(methodSymbol, invocation, out syntaxInfo);
        }

        return false;
    }

    private static bool TryHandleMessageBus(
        IMethodSymbol methodSymbol,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        syntaxInfo = null;

        var methodName = methodSymbol.Name;
        CallSiteKind kind;

        switch (methodName)
        {
            case "PublishAsync":
                kind = CallSiteKind.Publish;
                break;
            case "SendAsync":
                kind = CallSiteKind.Send;
                break;
            case "SchedulePublishAsync":
                kind = CallSiteKind.SchedulePublish;
                break;
            case "ScheduleSendAsync":
                kind = CallSiteKind.ScheduleSend;
                break;
            case "RequestAsync":
                kind = CallSiteKind.Request;
                break;
            default:
                return false;
        }

        // Non-generic RequestAsync(object, CT) - ack-only version.
        // Fall back to argument-expression analysis to get the compile-time type.
        if (kind == CallSiteKind.Request && methodSymbol.TypeArguments.Length == 0)
        {
            var argType = GetFirstArgumentType(invocation, semanticModel, cancellationToken);
            if (argType is not null
                && argType.SpecialType != SpecialType.System_Object
                && !IsOpenTypeParameter(argType))
            {
                var argTypeName = argType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var argLocation = invocation.GetLocation().ToLocationInfo();
                syntaxInfo = new CallSiteMessageTypeInfo(argTypeName, kind, argLocation);
                return true;
            }

            return false;
        }

        // All other IMessageBus methods are generic - extract T from type arguments.
        if (methodSymbol.TypeArguments.Length == 0)
        {
            return false;
        }

        var messageType = methodSymbol.TypeArguments[0];

        // For RequestAsync<TResponse>(IEventRequest<TResponse>, ...), the type argument is the response.
        // We want the message type (the first parameter's compile-time type) AND the response type.
        if (kind == CallSiteKind.Request
            && methodSymbol.Parameters.Length > 0
            && methodSymbol.Parameters[0].Type is INamedTypeSymbol)
        {
            // The first parameter is IEventRequest<TResponse> or a concrete type implementing it.
            // Get the compile-time type of the first argument expression.
            var firstArgType = GetFirstArgumentType(invocation, semanticModel, cancellationToken);
            if (firstArgType is not null && !IsOpenTypeParameter(firstArgType))
            {
                var requestTypeName = firstArgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var location = invocation.GetLocation().ToLocationInfo();

                // The response type is the type argument TResponse from RequestAsync<TResponse>.
                string? responseTypeName = null;
                if (methodSymbol.TypeArguments.Length > 0
                    && !IsOpenTypeParameter(methodSymbol.TypeArguments[0]))
                {
                    responseTypeName = methodSymbol
                        .TypeArguments[0]
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }

                syntaxInfo = new CallSiteMessageTypeInfo(requestTypeName, kind, location, responseTypeName);
                return true;
            }

            return false;
        }

        if (IsOpenTypeParameter(messageType))
        {
            return false;
        }

        var typeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var locationInfo = invocation.GetLocation().ToLocationInfo();
        syntaxInfo = new CallSiteMessageTypeInfo(typeName, kind, locationInfo);
        return true;
    }

    private static bool TryHandleSender(
        IMethodSymbol methodSymbol,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        syntaxInfo = null;

        var methodName = methodSymbol.Name;
        CallSiteKind kind;

        switch (methodName)
        {
            case "SendAsync":
                kind = CallSiteKind.MediatorSend;
                break;
            case "QueryAsync":
                kind = CallSiteKind.MediatorQuery;
                break;
            default:
                return false;
        }

        // ISender methods take the message as the first parameter.
        // For SendAsync(ICommand, CT) and SendAsync<TResponse>(ICommand<TResponse>, CT)
        // and QueryAsync<TResponse>(IQuery<TResponse>, CT), get the first argument's type.
        // Skip SendAsync(object, CT) - runtime dispatch, no static type info.
        if (methodSymbol.Parameters.Length == 0)
        {
            return false;
        }

        var firstParamType = methodSymbol.Parameters[0].Type;
        if (firstParamType.SpecialType == SpecialType.System_Object)
        {
            return false;
        }

        var messageType = GetFirstArgumentType(invocation, semanticModel, cancellationToken);
        if (messageType is null || IsOpenTypeParameter(messageType))
        {
            return false;
        }

        var typeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var locationInfo = invocation.GetLocation().ToLocationInfo();
        syntaxInfo = new CallSiteMessageTypeInfo(typeName, kind, locationInfo);
        return true;
    }

    private static bool TryHandlePublisher(
        IMethodSymbol methodSymbol,
        InvocationExpressionSyntax invocation,
        out SyntaxInfo? syntaxInfo)
    {
        syntaxInfo = null;

        if (methodSymbol.Name != "PublishAsync")
        {
            return false;
        }

        // Skip PublishAsync(object, CT) - runtime dispatch.
        if (methodSymbol.TypeArguments.Length == 0)
        {
            return false;
        }

        var messageType = methodSymbol.TypeArguments[0];
        if (IsOpenTypeParameter(messageType))
        {
            return false;
        }

        var typeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var locationInfo = invocation.GetLocation().ToLocationInfo();
        syntaxInfo = new CallSiteMessageTypeInfo(typeName, CallSiteKind.MediatorPublish, locationInfo);
        return true;
    }

    private static ITypeSymbol? GetFirstArgumentType(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        var firstArgExpression = invocation.ArgumentList.Arguments[0].Expression;
        var typeInfo = semanticModel.GetTypeInfo(firstArgExpression, cancellationToken);
        return typeInfo.Type;
    }

    private static bool IsOpenTypeParameter(ITypeSymbol type) => type.TypeKind == TypeKind.TypeParameter;
}
