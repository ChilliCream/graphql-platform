using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ObjectTypeInfo
    : SyntaxInfo
    , IOutputTypeInfo
{
    public ObjectTypeInfo(INamedTypeSymbol schemaType,
        INamedTypeSymbol runtimeType,
        Resolver? nodeResolver,
        ClassDeclarationSyntax classDeclarationSyntax,
        ImmutableArray<Resolver> resolvers)
    {
        SchemaSchemaType = schemaType;
        SchemaTypeFullName = schemaType.ToDisplayString();
        RuntimeType = runtimeType;
        RuntimeTypeFullName = runtimeType.ToDisplayString();
        NodeResolver = nodeResolver;
        ClassDeclaration = classDeclarationSyntax;
        Resolvers = resolvers;
    }

    public string Name => SchemaSchemaType.Name;

    public string Namespace => SchemaSchemaType.ContainingNamespace.ToDisplayString();

    public bool IsPublic => SchemaSchemaType.DeclaredAccessibility == Accessibility.Public;

    public bool IsRootType => false;

    public INamedTypeSymbol SchemaSchemaType { get; }

    public string SchemaTypeFullName { get; }

    public bool HasSchemaType => true;

    public INamedTypeSymbol RuntimeType { get; }

    public string RuntimeTypeFullName { get; }

    public bool HasRuntimeType => true;

    public Resolver? NodeResolver { get; }

    public ClassDeclarationSyntax ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; private set; }

    public override string OrderByKey => SchemaTypeFullName;

    public void ReplaceResolver(Resolver current, Resolver replacement)
        => Resolvers = Resolvers.Replace(current, replacement);

    public override bool Equals(object? obj)
        => obj is ObjectTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is ObjectTypeInfo other && Equals(other);

    private bool Equals(ObjectTypeInfo other)
        => string.Equals(SchemaTypeFullName, other.SchemaTypeFullName, StringComparison.Ordinal)
            && ClassDeclaration.SyntaxTree.IsEquivalentTo(
                other.ClassDeclaration.SyntaxTree);
    public override int GetHashCode()
        => HashCode.Combine(SchemaTypeFullName, ClassDeclaration);
}
