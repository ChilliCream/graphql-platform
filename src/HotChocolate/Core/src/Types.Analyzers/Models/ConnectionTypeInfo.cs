using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.Types.Analyzers.Inspectors.ObjectTypeInspector;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ConnectionTypeInfo
    : SyntaxInfo
    , IOutputTypeInfo
{
    private ConnectionTypeInfo(
        string name,
        string? nameFormat,
        string edgeTypeName,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        ImmutableArray<Resolver> resolvers)
    {
        Name = name;
        NameFormat = nameFormat;
        EdgeTypeName = edgeTypeName;
        RuntimeTypeFullName = runtimeType.ToDisplayString();
        RuntimeType = runtimeType;
        Namespace = runtimeType.ContainingNamespace.ToDisplayString();
        ClassDeclaration = classDeclaration;
        Resolvers = resolvers;
    }

    public string Name { get; }

    public string? NameFormat { get; }

    public string EdgeTypeName { get; }

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

    public static ConnectionTypeInfo CreateConnectionFrom(
        ConnectionClassInfo connectionClass,
        string edgeTypeName,
        string? name = null,
        string? nameFormat = null)
    {
        return new ConnectionTypeInfo(
            (name ?? connectionClass.RuntimeType.Name) + "Type",
            nameFormat,
            edgeTypeName,
            connectionClass.RuntimeType,
            connectionClass.ClassDeclarations,
            connectionClass.Resolvers);
    }

    public static ConnectionTypeInfo CreateConnection(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        string edgeTypeName,
        string? name = null,
        string? nameFormat = null)
        => Create(compilation, runtimeType, classDeclaration, edgeTypeName, name, nameFormat);

    private static ConnectionTypeInfo Create(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        string edgeTypeName,
        string? name = null,
        string? nameFormat = null)
    {
        var connectionName = (name ?? runtimeType.Name) + "Type";

        var resolvers = ImmutableArray.CreateBuilder<Resolver>();

        foreach (var member in runtimeType.AllPublicInstanceMembers())
        {
            switch (member)
            {
                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                    resolvers.Add(CreateResolver(compilation, runtimeType, method, connectionName));
                    break;

                case IPropertySymbol property:
                    var flags = FieldFlags.None;

                    if (property.Name.Equals("Edges", StringComparison.Ordinal))
                    {
                        flags |= FieldFlags.ConnectionEdgesField;
                    }
                    else if (property.Name.Equals("Nodes", StringComparison.Ordinal))
                    {
                        flags |= FieldFlags.ConnectionNodesField;
                    }
                    else if (property.Name.Equals("TotalCount", StringComparison.Ordinal))
                    {
                        flags |= FieldFlags.TotalCount;
                    }

                    resolvers.Add(
                        new Resolver(
                            connectionName,
                            property,
                            ResolverResultKind.Pure,
                            [],
                            GetMemberBindings(member),
                            flags: flags));
                    break;
            }
        }

        return new ConnectionTypeInfo(
            connectionName,
            nameFormat,
            edgeTypeName,
            runtimeType,
            classDeclaration,
            resolvers.ToImmutable());
    }
}
