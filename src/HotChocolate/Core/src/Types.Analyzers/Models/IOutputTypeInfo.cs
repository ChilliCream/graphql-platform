using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public interface IOutputTypeInfo
{
    string Name { get; }

    INamedTypeSymbol Type { get; }

    INamedTypeSymbol RuntimeType { get; }

    ClassDeclarationSyntax ClassDeclarationSyntax { get; }

    ImmutableArray<Resolver> Resolvers { get; }

    ImmutableArray<Diagnostic> Diagnostics { get; }

    void AddDiagnostic(Diagnostic diagnostic);

    void AddDiagnosticRange(ImmutableArray<Diagnostic> diagnostics);
}
