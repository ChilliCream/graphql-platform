using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Represents an inspector that detects concrete class or record declarations implementing
/// command or query handler interfaces such as <c>ICommandHandler</c> or <c>IQueryHandler</c>.
/// </summary>
public sealed class HandlerInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [ClassWithMochaBaseListFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } =
        ImmutableHashSet.Create(SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration);

    private static readonly HandlerKindDescriptor[] s_handlerKinds =
    [
        new(static s => s.ICommandHandlerVoid, HandlerKind.CommandVoid, HasResponse: false),
        new(static s => s.ICommandHandlerResponse, HandlerKind.CommandResponse, HasResponse: true),
        new(static s => s.IQueryHandler, HandlerKind.Query, HasResponse: true)
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

        if (namedTypeSymbol is null
            || namedTypeSymbol.IsAbstract
            || namedTypeSymbol.TypeKind == TypeKind.Interface)
        {
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

            syntaxInfo = new HandlerInfo(
                handlerFullName,
                handlerNamespace,
                implemented.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                descriptor.HasResponse
                    ? implemented.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : null,
                descriptor.Kind);
            return true;
        }

        return false;
    }

    private sealed record HandlerKindDescriptor(
        Func<KnownTypeSymbols, INamedTypeSymbol?> GetTarget,
        HandlerKind Kind,
        bool HasResponse);
}
