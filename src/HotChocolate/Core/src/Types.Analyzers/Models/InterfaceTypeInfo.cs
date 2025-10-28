using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class InterfaceTypeInfo : SyntaxInfo, IOutputTypeInfo
{
    public InterfaceTypeInfo(
        INamedTypeSymbol schemaType,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax classDeclarationSyntax,
        ImmutableArray<Resolver> resolvers,
        ImmutableArray<AttributeData> attributes)
    {
        SchemaSchemaType = schemaType;
        SchemaTypeFullName = schemaType.ToDisplayString();
        RuntimeType = runtimeType;
        RuntimeTypeFullName = runtimeType.ToDisplayString();
        ClassDeclaration = classDeclarationSyntax;
        Resolvers = resolvers;
        Description = schemaType.GetDescription();
        // sharable directives are only allowed on object types and field definitions
        Shareable = DirectiveScope.None;
        Inaccessible = attributes.GetInaccessibleScope();
        Attributes = attributes.GetUserAttributes();
    }

    public string Name => SchemaSchemaType.Name;

    public string Namespace => SchemaSchemaType.ContainingNamespace.ToDisplayString();

    public string? Description { get; }

    public bool IsPublic => SchemaSchemaType.DeclaredAccessibility == Accessibility.Public;

    public INamedTypeSymbol SchemaSchemaType { get; }

    public string SchemaTypeFullName { get; }

    public bool HasSchemaType => true;

    public INamedTypeSymbol RuntimeType { get; }

    public string? RuntimeTypeFullName { get; }

    public bool HasRuntimeType => true;

    public ClassDeclarationSyntax ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; private set; }

    public override string OrderByKey => SchemaTypeFullName;

    public DirectiveScope Shareable { get; }

    public DirectiveScope Inaccessible { get; }

    public ImmutableArray<AttributeData> Attributes { get; }

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
