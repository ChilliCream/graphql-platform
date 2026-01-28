using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
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
        ImmutableArray<Resolver> resolvers,
        ImmutableArray<AttributeData> attributes)
    {
        Name = schemaType.Name;
        OperationType = operationType;
        SchemaTypeName = TypeNameInfo.Create(schemaType);
        RegistrationKey = schemaType.ToAssemblyQualified();
        Namespace = schemaType.ContainingNamespace.ToDisplayString();
        Description = schemaType.GetDescription();
        IsPublic = schemaType.DeclaredAccessibility == Accessibility.Public;
        ClassDeclaration = classDeclarationSyntax;
        Resolvers = resolvers;
        Shareable = attributes.GetShareableScope();
        Inaccessible = attributes.GetInaccessibleScope();
        DescriptorAttributes = attributes.GetUserAttributes();
    }

    public string Name { get; }

    public TypeNameInfo SchemaTypeName { get; }

    public TypeNameInfo? RuntimeTypeName => null;

    public string RegistrationKey { get; }

    public string Namespace { get; }

    public string? Description { get; }

    public bool IsPublic { get; }

    public OperationType OperationType { get; }

    public bool HasSchemaType => true;

    public bool HasRuntimeType => false;

    public ClassDeclarationSyntax ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; private set; }

    public override string OrderByKey => SchemaTypeName.FullName;

    public DirectiveScope Shareable { get; }

    public DirectiveScope Inaccessible { get; }

    public ImmutableArray<AttributeData> DescriptorAttributes { get; }

    public bool SourceSchemaDetected { get; set; }

    public void ReplaceResolver(Resolver current, Resolver replacement)
        => Resolvers = Resolvers.Replace(current, replacement);

    public override bool Equals(object? obj)
        => obj is RootTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is RootTypeInfo other && Equals(other);

    public bool Equals(RootTypeInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return OrderByKey.Equals(other.OrderByKey, StringComparison.Ordinal)
            && string.Equals(SchemaTypeName.FullName, other.SchemaTypeName.FullName, StringComparison.Ordinal)
            && ClassDeclaration.SyntaxTree.IsEquivalentTo(other.ClassDeclaration.SyntaxTree);
    }

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, SchemaTypeName.FullName, ClassDeclaration);
}
