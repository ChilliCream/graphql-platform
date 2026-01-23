using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class InterfaceTypeInfo
    : SyntaxInfo
    , IOutputTypeInfo
{
    public InterfaceTypeInfo(
        INamedTypeSymbol schemaType,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax classDeclarationSyntax,
        ImmutableArray<Resolver> resolvers,
        ImmutableArray<AttributeData> attributes)
    {
        Name = schemaType.Name;
        SchemaTypeName = TypeNameInfo.Create(schemaType);
        RuntimeTypeName = TypeNameInfo.Create(runtimeType);
        RegistrationKey = schemaType.ToAssemblyQualified();
        Namespace = schemaType.ContainingNamespace.ToDisplayString();
        Description = schemaType.GetDescription();
        IsPublic = schemaType.DeclaredAccessibility == Accessibility.Public;
        ClassDeclaration = classDeclarationSyntax;
        Resolvers = resolvers;
        // sharable directives are only allowed on object types and field definitions
        Shareable = DirectiveScope.None;
        Inaccessible = attributes.GetInaccessibleScope();
        DescriptorAttributes = attributes.GetUserAttributes();
    }

    public string Name { get; }

    public TypeNameInfo SchemaTypeName { get; }

    public TypeNameInfo RuntimeTypeName { get; }

    public string RegistrationKey { get; }

    public string Namespace { get; }

    public string? Description { get; }

    public bool IsPublic { get; }

    public bool HasSchemaType => true;

    public bool HasRuntimeType => true;

    public ClassDeclarationSyntax ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; private set; }

    public override string OrderByKey => SchemaTypeName.FullName;

    public DirectiveScope Shareable { get; }

    public DirectiveScope Inaccessible { get; }

    public ImmutableArray<AttributeData> DescriptorAttributes { get; }

    public void ReplaceResolver(Resolver current, Resolver replacement)
        => Resolvers = Resolvers.Replace(current, replacement);

    public override bool Equals(object? obj)
        => obj is InterfaceTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is InterfaceTypeInfo other && Equals(other);

    private bool Equals(InterfaceTypeInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return OrderByKey.Equals(other.OrderByKey)
            && string.Equals(SchemaTypeName.FullName, other.SchemaTypeName.FullName, StringComparison.Ordinal)
            && ClassDeclaration.SyntaxTree.IsEquivalentTo(other.ClassDeclaration.SyntaxTree);
    }

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, SchemaTypeName.FullName, ClassDeclaration);
}
