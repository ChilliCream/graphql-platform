using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

public sealed class MultipleHandlerInterfaceInspector : ISyntaxInspector
{
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [ClassWithMochaBaseListFilter.Instance];

    public IImmutableSet<SyntaxKind> SupportedKinds { get; } =
        ImmutableHashSet.Create(SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration);

    public bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        if (node is not TypeDeclarationSyntax typeDeclaration)
        {
            syntaxInfo = null;
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

        if (typeSymbol is null
            || typeSymbol.IsAbstract
            || typeSymbol.TypeKind == TypeKind.Interface)
        {
            syntaxInfo = null;
            return false;
        }

        var count = 0;

        foreach (var @interface in typeSymbol.Interfaces)
        {
            var original = @interface.OriginalDefinition;

            if (SymbolEqualityComparer.Default.Equals(original, knownSymbols.ICommandHandlerVoid)
                || SymbolEqualityComparer.Default.Equals(original, knownSymbols.ICommandHandlerResponse)
                || SymbolEqualityComparer.Default.Equals(original, knownSymbols.IQueryHandler)
                || SymbolEqualityComparer.Default.Equals(original, knownSymbols.INotificationHandler))
            {
                count++;

                if (count > 1)
                {
                    break;
                }
            }
        }

        if (count <= 1)
        {
            syntaxInfo = null;
            return false;
        }

        var handlerName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var locationInfo = typeDeclaration.Identifier.GetLocation().ToLocationInfo();

        syntaxInfo = new MultipleHandlerInterfaceDiagnosticInfo(handlerName)
        {
            Diagnostics = new([
                new DiagnosticInfo(
                    Errors.MultipleHandlerInterfaces.Id,
                    locationInfo,
                    new([handlerName]))
            ])
        };
        return true;
    }
}

internal sealed record MultipleHandlerInterfaceDiagnosticInfo(string HandlerTypeName) : SyntaxInfo
{
    public override string OrderByKey => $"MultipleHandlerDiag:{HandlerTypeName}";
}
