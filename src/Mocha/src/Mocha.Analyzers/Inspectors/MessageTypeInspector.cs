using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Inspectors;

/// <summary>
/// Represents an inspector that detects concrete type declarations (classes, records, and structs)
/// implementing message interfaces such as <c>ICommand</c>, <c>ICommand&lt;T&gt;</c>,
/// or <c>IQuery&lt;T&gt;</c>.
/// </summary>
public sealed class MessageTypeInspector : ISyntaxInspector
{
    /// <inheritdoc />
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [ClassWithMochaBaseListFilter.Instance];

    /// <inheritdoc />
    public IImmutableSet<SyntaxKind> SupportedKinds { get; } =
        ImmutableHashSet.Create(
            SyntaxKind.ClassDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordStructDeclaration);

    /// <inheritdoc />
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

        if (typeSymbol is not { } namedTypeSymbol)
        {
            syntaxInfo = null;
            return false;
        }

        // Skip abstract types and interfaces - they are not concrete message types
        if (namedTypeSymbol.IsAbstract || namedTypeSymbol.TypeKind == TypeKind.Interface)
        {
            syntaxInfo = null;
            return false;
        }

        // Open generic types cannot be dispatched at runtime - report MO0004
        if (namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeParameters.Length > 0
            && SymbolEqualityComparer.Default.Equals(namedTypeSymbol, namedTypeSymbol.OriginalDefinition))
        {
            if (ImplementsAnyMessageInterface(knownSymbols, namedTypeSymbol))
            {
                var openTypeName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var locationInfo = typeDeclaration.Identifier.GetLocation().ToLocationInfo();

                syntaxInfo = new OpenGenericMessageDiagnosticInfo(openTypeName)
                {
                    Diagnostics = new([
                        new DiagnosticInfo(
                            Errors.OpenGenericMessageType.Id,
                            locationInfo,
                            new([openTypeName]))
                    ])
                };
                return true;
            }

            syntaxInfo = null;
            return false;
        }

        var typeName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeNamespace = namedTypeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var location = typeDeclaration.Identifier.GetLocation().ToLocationInfo();

        // Try ICommand (void)
        if (knownSymbols.ICommandVoid is not null
            && namedTypeSymbol.ImplementsInterface(knownSymbols.ICommandVoid))
        {
            // Check if it also implements ICommand<T> - if so, treat as CommandResponse
            if (knownSymbols.ICommandOfT is not null
                && namedTypeSymbol.FindImplementedInterface(knownSymbols.ICommandOfT) is not null)
            {
                syntaxInfo = new MessageTypeInfo(typeName, typeNamespace, MessageKind.CommandResponse, location);
                return true;
            }

            syntaxInfo = new MessageTypeInfo(typeName, typeNamespace, MessageKind.Command, location);
            return true;
        }

        // Try ICommand<T>
        if (knownSymbols.ICommandOfT is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.ICommandOfT) is not null)
        {
            syntaxInfo = new MessageTypeInfo(typeName, typeNamespace, MessageKind.CommandResponse, location);
            return true;
        }

        // Try IQuery<T>
        if (knownSymbols.IQueryOfT is not null
            && namedTypeSymbol.FindImplementedInterface(knownSymbols.IQueryOfT) is not null)
        {
            syntaxInfo = new MessageTypeInfo(typeName, typeNamespace, MessageKind.Query, location);
            return true;
        }

        syntaxInfo = null;
        return false;
    }

    private static bool ImplementsAnyMessageInterface(KnownTypeSymbols knownSymbols, INamedTypeSymbol type)
    {
        if (knownSymbols.ICommandVoid is not null && type.ImplementsInterface(knownSymbols.ICommandVoid))
        {
            return true;
        }

        if (knownSymbols.ICommandOfT is not null && type.FindImplementedInterface(knownSymbols.ICommandOfT) is not null)
        {
            return true;
        }

        if (knownSymbols.IQueryOfT is not null && type.FindImplementedInterface(knownSymbols.IQueryOfT) is not null)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// A diagnostic-only SyntaxInfo used to carry MO0004 diagnostics for open generic message types.
/// This is not used by code generators.
/// </summary>
internal sealed record OpenGenericMessageDiagnosticInfo(string MessageTypeName) : SyntaxInfo
{
    public override string OrderByKey => $"OpenGenericDiag:{MessageTypeName}";
}
