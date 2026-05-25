using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Inspects concrete class declarations to discover <c>Saga&lt;TState&gt;</c> subclasses
/// for source-generated registration via <c>AddSaga&lt;T&gt;</c>.
/// </summary>
public sealed class SagaInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [ClassWithMochaBaseListFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } =
        ImmutableHashSet.Create(SyntaxKind.ClassDeclaration);

    /// <inheritdoc />
    public bool TryHandle(
        KnownTypeSymbols knownSymbols,
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out SyntaxInfo? syntaxInfo)
    {
        syntaxInfo = null;

        if (knownSymbols.Saga is null)
        {
            return false;
        }

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

        // Walk the base type chain looking for Saga<TState>
        if (!DerivesFromSaga(namedTypeSymbol, knownSymbols.Saga))
        {
            return false;
        }

        var sagaFullName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sagaNamespace = namedTypeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        // Check for public parameterless constructor (MO0014)
        if (!HasPublicParameterlessConstructor(namedTypeSymbol))
        {
            var locationInfo = typeDeclaration.Identifier.GetLocation().ToLocationInfo();
            syntaxInfo = new SagaInfo(sagaFullName, sagaNamespace)
            {
                Diagnostics = new([
                    new DiagnosticInfo(
                        Errors.SagaMissingParameterlessConstructor.Id,
                        locationInfo,
                        new([sagaFullName]))
                ])
            };
            return true;
        }

        syntaxInfo = new SagaInfo(sagaFullName, sagaNamespace);
        return true;
    }

    private static bool DerivesFromSaga(INamedTypeSymbol type, INamedTypeSymbol sagaSymbol)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType
                && SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, sagaSymbol))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
    {
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility == Accessibility.Public
                && constructor.Parameters.Length == 0)
            {
                return true;
            }
        }

        return false;
    }
}
