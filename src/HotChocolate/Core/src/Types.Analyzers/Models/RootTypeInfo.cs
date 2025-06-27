using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class RootTypeInfo
    : SyntaxInfo
    , IOutputTypeInfo
{
    public RootTypeInfo(
        INamedTypeSymbol schemaType,
        OperationType operationType,
        ClassDeclarationSyntax classDeclarationSyntax,
        ImmutableArray<Resolver> resolvers)
    {
        OperationType = operationType;
        SchemaSchemaType = schemaType;
        SchemaTypeFullName = schemaType.ToDisplayString();
        ClassDeclaration = classDeclarationSyntax;
        Resolvers = resolvers;
    }

    public string Name => SchemaSchemaType.Name;

    public string Namespace => SchemaSchemaType.ContainingNamespace.ToDisplayString();

    public bool IsPublic => SchemaSchemaType.DeclaredAccessibility == Accessibility.Public;

    public OperationType OperationType { get; }

    public INamedTypeSymbol SchemaSchemaType { get; }

    public string SchemaTypeFullName { get; }

    public bool HasSchemaType => true;

    public INamedTypeSymbol? RuntimeType => null;

    public string? RuntimeTypeFullName => null;

    public bool HasRuntimeType => false;

    public ClassDeclarationSyntax ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; private set; }

    public override string OrderByKey => SchemaTypeFullName;

    public void ReplaceResolver(Resolver current, Resolver replacement)
        => Resolvers = Resolvers.Replace(current, replacement);

    public override bool Equals(object? obj)
        => obj is ObjectTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is ObjectTypeInfo other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(SchemaTypeFullName, ClassDeclaration);
}
