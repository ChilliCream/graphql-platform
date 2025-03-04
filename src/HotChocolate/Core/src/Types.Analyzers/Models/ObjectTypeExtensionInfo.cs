using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ObjectTypeExtensionInfo : SyntaxInfo
    , IOutputTypeInfo
{
    public ObjectTypeExtensionInfo(INamedTypeSymbol schemaType,
        INamedTypeSymbol runtimeType,
        Resolver? nodeResolver,
        ClassDeclarationSyntax classDeclarationSyntax,
        ImmutableArray<Resolver> resolvers)
    {
        SchemaSchemaType = schemaType;
        SchemaTypeFullName = schemaType.ToFullyQualified();
        RuntimeType = runtimeType;
        RuntimeTypeFullName = runtimeType.ToFullyQualified();
        NodeResolver = nodeResolver;
        ClassDeclaration = classDeclarationSyntax;
        Resolvers = resolvers;
    }

    public string Name => SchemaSchemaType.Name;

    public string Namespace => SchemaSchemaType.ContainingNamespace.ToDisplayString();

    public bool IsRootType => false;

    public INamedTypeSymbol SchemaSchemaType { get; }

    public string SchemaTypeFullName { get; }

    public bool HasSchemaType => true;

    public INamedTypeSymbol RuntimeType { get; }

    public string RuntimeTypeFullName { get; }

    public bool HasRuntimeType => true;

    public Resolver? NodeResolver { get; }

    public ClassDeclarationSyntax ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; }

    public override string OrderByKey => SchemaTypeFullName;

    public override bool Equals(object? obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is ObjectTypeExtensionInfo other && Equals(other);

    private bool Equals(ObjectTypeExtensionInfo other)
        => string.Equals(SchemaTypeFullName, other.SchemaTypeFullName, StringComparison.Ordinal)
            && ClassDeclaration.SyntaxTree.IsEquivalentTo(
                other.ClassDeclaration.SyntaxTree);
    public override int GetHashCode()
        => HashCode.Combine(SchemaTypeFullName, ClassDeclaration);
}
