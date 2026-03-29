using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Inspects concrete class or record declarations to discover MessageBus handlers
/// (<c>IEventHandler</c>, <c>IEventRequestHandler</c>, <c>IConsumer</c>, <c>IBatchEventHandler</c>)
/// using a priority cascade where the first matching interface wins.
/// </summary>
public sealed class MessagingHandlerInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [ClassWithMochaBaseListFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } =
        ImmutableHashSet.Create(SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration);

    // Priority cascade: first match wins (matches runtime MessageBusBuilder.AddHandler<T> logic)
    private static readonly MessagingHandlerKindDescriptor[] s_handlerKinds =
    [
        new(static s => s.IBatchEventHandler, MessagingHandlerKind.Batch, false, 0),
        new(static s => s.IConsumer, MessagingHandlerKind.Consumer, false, 0),
        new(static s => s.IEventRequestHandlerResponse, MessagingHandlerKind.RequestResponse, true, 0),
        new(static s => s.IEventRequestHandlerVoid, MessagingHandlerKind.Send, false, 0),
        new(static s => s.IEventHandler, MessagingHandlerKind.Event, false, 0)
    ];

    /// <inheritdoc />
    public bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        syntaxInfo = null;

        if (node is not TypeDeclarationSyntax typeDeclaration)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var namedTypeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

        if (namedTypeSymbol is null || namedTypeSymbol.IsAbstract || namedTypeSymbol.TypeKind == TypeKind.Interface)
        {
            return false;
        }

        // Check for open generics (MO0012)
        if (namedTypeSymbol is { IsGenericType: true, TypeParameters.Length: > 0 })
        {
            if (ImplementsAnyMessagingInterface(knownSymbols, namedTypeSymbol))
            {
                var handlerName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var locationInfo = typeDeclaration.Identifier.GetLocation().ToLocationInfo();
                syntaxInfo = new OpenGenericMessagingHandlerDiagnosticInfo(handlerName)
                {
                    Diagnostics = new([
                        new DiagnosticInfo(Errors.OpenGenericMessagingHandler.Id, locationInfo, new([handlerName]))
                    ])
                };
                return true;
            }

            return false;
        }

        foreach (var descriptor in s_handlerKinds)
        {
            var target = descriptor.GetTarget(knownSymbols);
            var implemented = target is not null
                ? namedTypeSymbol.FindImplementedInterface(target)
                : null;

            if (implemented is null)
            {
                continue;
            }

            var handlerFullName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerNamespace = namedTypeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            var messageTypeName = implemented.TypeArguments[descriptor.MessageTypeArgIndex]
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var locationInfo = typeDeclaration.Identifier.GetLocation().ToLocationInfo();

            syntaxInfo = new MessagingHandlerInfo(
                handlerFullName,
                handlerNamespace,
                messageTypeName,
                descriptor.HasResponse
                    ? implemented.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : null,
                descriptor.Kind,
                locationInfo);
            return true;
        }

        return false;
    }

    private static bool ImplementsAnyMessagingInterface(
        KnownTypeSymbols knownSymbols,
        INamedTypeSymbol namedTypeSymbol)
    {
        return
            (knownSymbols.IBatchEventHandler is not null
                && namedTypeSymbol.FindImplementedInterface(knownSymbols.IBatchEventHandler) is not null)
            || (knownSymbols.IConsumer is not null
                && namedTypeSymbol.FindImplementedInterface(knownSymbols.IConsumer) is not null)
            || (knownSymbols.IEventRequestHandlerResponse is not null
                && namedTypeSymbol.FindImplementedInterface(knownSymbols.IEventRequestHandlerResponse) is not null)
            || (knownSymbols.IEventRequestHandlerVoid is not null
                && namedTypeSymbol.FindImplementedInterface(knownSymbols.IEventRequestHandlerVoid) is not null)
            || (knownSymbols.IEventHandler is not null
                && namedTypeSymbol.FindImplementedInterface(knownSymbols.IEventHandler) is not null);
    }

    private sealed record MessagingHandlerKindDescriptor(
        Func<KnownTypeSymbols, INamedTypeSymbol?> GetTarget,
        MessagingHandlerKind Kind,
        bool HasResponse,
        int MessageTypeArgIndex);
}

/// <summary>
/// A diagnostic-only SyntaxInfo for MO0012 (open generic messaging handler).
/// This is not used by code generators.
/// </summary>
internal sealed record OpenGenericMessagingHandlerDiagnosticInfo(string HandlerTypeName) : SyntaxInfo
{
    /// <inheritdoc />
    public override string OrderByKey => $"MsgOpenGenericDiag:{HandlerTypeName}";
}
