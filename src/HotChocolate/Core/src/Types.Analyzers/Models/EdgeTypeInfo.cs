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
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        ImmutableArray<Resolver> resolvers)
    {
        Name = name;
        NameFormat = nameFormat;
        RuntimeTypeFullName = runtimeType.ToDisplayString();
        RuntimeType = runtimeType;
        Namespace = @namespace;
        ClassDeclaration = classDeclaration;
        Resolvers = resolvers;
    }

    public string Name { get; }

    public string? NameFormat { get; }

    public string Namespace { get; }

    public bool IsPublic => RuntimeType.DeclaredAccessibility == Accessibility.Public;

    public INamedTypeSymbol? SchemaSchemaType => null;

    public string? SchemaTypeFullName => null;

    public bool HasSchemaType => false;

    public INamedTypeSymbol RuntimeType { get; }

    public string RuntimeTypeFullName { get; }

    public bool HasRuntimeType => true;

    public ClassDeclarationSyntax? ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; private set; }

    public override string OrderByKey => RuntimeTypeFullName;

    public void ReplaceResolver(Resolver current, Resolver replacement)
        => Resolvers = Resolvers.Replace(current, replacement);

    public override bool Equals(object? obj)
        => obj is ConnectionTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is ConnectionTypeInfo other && Equals(other);

    private bool Equals(ConnectionTypeInfo other)
    {
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

        return ClassDeclaration.SyntaxTree.IsEquivalentTo(
            other.ClassDeclaration.SyntaxTree);
    }

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, ClassDeclaration);

    public static EdgeTypeInfo CreateEdgeFrom(
        ConnectionClassInfo connectionClass,
        string @namespace,
        string? name = null,
        string? nameFormat = null)
    {
        return new EdgeTypeInfo(
            (name ?? connectionClass.RuntimeType.Name) + "Type",
            nameFormat,
            @namespace,
            connectionClass.RuntimeType,
            connectionClass.ClassDeclarations,
            connectionClass.Resolvers);
    }

    public static EdgeTypeInfo CreateEdge(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        string @namespace,
        string? name = null,
        string? nameFormat = null)
        => Create(compilation, runtimeType, classDeclaration, @namespace, name, nameFormat);

    private static EdgeTypeInfo Create(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
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
                            ResolverResultKind.Pure,
                            [],
                            ObjectTypeInspector.GetMemberBindings(member),
                            flags: FieldFlags.None));
                    break;
            }
        }

        return new EdgeTypeInfo(
            edgeName,
            nameFormat,
            @namespace,
            runtimeType,
            classDeclaration,
            resolvers.ToImmutable());
    }
}
