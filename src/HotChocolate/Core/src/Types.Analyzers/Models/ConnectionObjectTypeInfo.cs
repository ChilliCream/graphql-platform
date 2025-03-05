using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.Types.Analyzers.Inspectors.ObjectTypeExtensionInfoInspector;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ConnectionObjectTypeInfo
    : SyntaxInfo
    , IOutputTypeInfo
{
    private ConnectionObjectTypeInfo(
        string name,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        ImmutableArray<Resolver> resolvers)
    {
        Name = name;
        RuntimeTypeFullName = runtimeType.ToDisplayString();
        RuntimeType = runtimeType;
        Namespace = runtimeType.ContainingNamespace.ToDisplayString();
        ClassDeclaration = classDeclaration;
        Resolvers = resolvers;
    }

    public string Name { get; }

    public string Namespace { get; }

    public bool IsPublic => RuntimeType.DeclaredAccessibility == Accessibility.Public;

    public INamedTypeSymbol? SchemaSchemaType => null;

    public string? SchemaTypeFullName => null;

    public bool HasSchemaType => false;

    public INamedTypeSymbol RuntimeType { get; }

    public string RuntimeTypeFullName { get; }

    public bool HasRuntimeType => true;

    public ClassDeclarationSyntax? ClassDeclaration { get; }

    public ImmutableArray<Resolver> Resolvers { get; }

    public override string OrderByKey => RuntimeTypeFullName;

    public override bool Equals(object? obj)
        => obj is ConnectionObjectTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? obj)
        => obj is ConnectionObjectTypeInfo other && Equals(other);

    private bool Equals(ConnectionObjectTypeInfo other)
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


    public static ConnectionObjectTypeInfo CreateConnectionFrom(
        ConnectionClassInfo connectionClass)
    {
        return new ConnectionObjectTypeInfo(
            connectionClass.RuntimeType.Name + "Type",
            connectionClass.RuntimeType,
            connectionClass.ClassDeclarations,
            connectionClass.Resolvers);
    }

    public static ConnectionObjectTypeInfo CreateConnection(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration)
        => Create(compilation, runtimeType, classDeclaration, isConnection: true);

    public static ConnectionObjectTypeInfo CreateEdge(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration)
        => Create(compilation, runtimeType, classDeclaration, isConnection: false);

    private static ConnectionObjectTypeInfo Create(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax? classDeclaration,
        bool isConnection)
    {
        var name = runtimeType.Name + "Type";

        var resolvers = ImmutableArray.CreateBuilder<Resolver>();

        foreach (var member in runtimeType.GetMembers())
        {
            if (member.DeclaredAccessibility is not Accessibility.Public
                || member.IsStatic
                || member.IsIgnored())
            {
                continue;
            }

            switch (member)
            {
                case IMethodSymbol method:
                    if (method.IsPropertyOrEventAccessor()
                        || method.IsOperator()
                        || method.IsConstructor()
                        || method.IsSpecialMethod()
                        || method.IsCompilerGenerated())
                    {
                        continue;
                    }

                    resolvers.Add(CreateResolver(compilation, runtimeType, method, name));
                    break;

                case IPropertySymbol property:
                    var flags = FieldFlags.None;

                    if (isConnection)
                    {
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
                    }

                    resolvers.Add(
                        new Resolver(
                            name,
                            property,
                            ResolverResultKind.Pure,
                            ImmutableArray<ResolverParameter>.Empty,
                            GetMemberBindings(member),
                            flags: flags));
                    break;
            }
        }

        return new ConnectionObjectTypeInfo(
            name,
            runtimeType,
            classDeclaration,
            resolvers.ToImmutable());
    }
}
