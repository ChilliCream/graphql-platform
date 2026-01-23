using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class EdgeTypeInfo
    : SyntaxInfo
    , IOutputTypeInfo
{
    private EdgeTypeInfo(
        string name,
        string? nameFormat,
        string @namespace,
        string? description,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        ImmutableArray<Resolver> resolvers,
        ImmutableArray<AttributeData> attributes)
    {
        Name = name;
        RuntimeTypeName = TypeNameInfo.Create(runtimeType);
        NodeFullyQualifiedName = runtimeType.IsGenericType ? runtimeType.TypeArguments[0].ToFullyQualified() : null;
        NameFormat = nameFormat;
        Namespace = @namespace;
        Description = description;
        IsPublic = runtimeType.DeclaredAccessibility == Accessibility.Public;
        ClassDeclaration = classDeclaration;
        Resolvers = resolvers;
        Shareable = attributes.GetShareableScope();
        Inaccessible = attributes.GetInaccessibleScope();
        DescriptorAttributes = attributes.GetUserAttributes();
    }

    public string Name { get; }

    public TypeNameInfo? SchemaTypeName => null;

    public TypeNameInfo RuntimeTypeName { get; }

    public string? NodeFullyQualifiedName { get; }

    public string? RegistrationKey => null;

    public string? NameFormat { get; }

    public string Namespace { get; }

    public string? Description { get; }

    public bool IsPublic { get; }

    public bool HasSchemaType => false;

    public bool HasRuntimeType => true;

    public ClassDeclarationSyntax? ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; private set; }

    public DirectiveScope Shareable { get; private set; }

    public DirectiveScope Inaccessible { get; private set; }

    public ImmutableArray<AttributeData> DescriptorAttributes { get; }

    public override string OrderByKey => RuntimeTypeName.FullName;

    public void ReplaceResolver(Resolver current, Resolver replacement)
        => Resolvers = Resolvers.Replace(current, replacement);

    public override bool Equals(object? obj)
        => obj is EdgeTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is EdgeTypeInfo other && Equals(other);

    private bool Equals(EdgeTypeInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(OrderByKey, other.OrderByKey, StringComparison.Ordinal))
        {
            return false;
        }

        if (ClassDeclaration is null)
        {
            return other.ClassDeclaration is null;
        }

        if (other.ClassDeclaration is null)
        {
            return false;
        }

        return ClassDeclaration.SyntaxTree.IsEquivalentTo(other.ClassDeclaration.SyntaxTree);
    }

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, ClassDeclaration);

    public static EdgeTypeInfo CreateEdgeFrom(
        ConnectionClassInfo connectionClass,
        string @namespace,
        string? name = null,
        string? nameFormat = null)
    {
        var attributes = connectionClass.RuntimeType.GetAttributes();

        return new EdgeTypeInfo(
            (name ?? connectionClass.RuntimeType.Name) + "Type",
            nameFormat,
            @namespace,
            null,
            connectionClass.RuntimeType,
            connectionClass.ClassDeclaration,
            connectionClass.Resolvers,
            [])
        {
            Shareable = attributes.GetShareableScope(),
            Inaccessible = attributes.GetInaccessibleScope()
        };
    }

    public static EdgeTypeInfo CreateEdge(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        ImmutableArray<AttributeData> attributes,
        string @namespace,
        string? name = null,
        string? nameFormat = null)
        => Create(compilation, runtimeType, classDeclaration, attributes, @namespace, name, nameFormat);

    private static EdgeTypeInfo Create(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        ImmutableArray<AttributeData> attributes,
        string @namespace,
        string? name = null,
        string? nameFormat = null)
    {
        var edgeName = (name ?? runtimeType.Name) + "Type";

        var resolvers = ImmutableArray.CreateBuilder<Resolver>();

        foreach (var member in runtimeType.AllPublicInstanceMembers())
        {
            switch (member)
            {
                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                    resolvers.Add(ObjectTypeInspector.CreateResolver(compilation, runtimeType, method, edgeName));
                    break;

                case IPropertySymbol property:
                    resolvers.Add(
                        new Resolver(
                            edgeName,
                            property,
                            compilation.GetDescription(property, []),
                            compilation.GetDeprecationReason(property),
                            ResolverResultKind.Pure,
                            [],
                            ObjectTypeInspector.GetMemberBindings(member),
                            compilation.CreateTypeReference(property),
                            flags: FieldFlags.None));
                    break;
            }
        }

        return new EdgeTypeInfo(
            edgeName,
            nameFormat,
            @namespace,
            runtimeType.GetDescription(),
            runtimeType,
            classDeclaration,
            resolvers.ToImmutable(),
            attributes);
    }
}
