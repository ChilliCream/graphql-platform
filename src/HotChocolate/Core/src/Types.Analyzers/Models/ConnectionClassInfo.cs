using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ConnectionClassInfo : SyntaxInfo, IEquatable<ConnectionClassInfo>
{
    private ConnectionClassInfo(
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax classDeclarations,
        ImmutableArray<Resolver> resolvers)
    {
        RuntimeType = runtimeType;
        ClassDeclarations = classDeclarations;
        Resolvers = resolvers;
        OrderByKey = runtimeType.ToFullyQualified();
    }

    public INamedTypeSymbol RuntimeType { get; }

    public ClassDeclarationSyntax ClassDeclarations { get; }

    public ImmutableArray<Resolver> Resolvers { get; }

    public override string OrderByKey { get; }

    public override bool Equals(object? obj)
        => obj is ConnectionClassInfo other
            && Equals(other);

    public override bool Equals(SyntaxInfo? other)
        => other is ConnectionClassInfo otherConnectionClassInfo
            && Equals(otherConnectionClassInfo);

    public bool Equals(ConnectionClassInfo? other)
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
            && ClassDeclarations.SyntaxTree.IsEquivalentTo(other.ClassDeclarations.SyntaxTree);
    }

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, ClassDeclarations);

    public static ConnectionClassInfo CreateConnection(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax classDeclaration)
        => Create(compilation, runtimeType, classDeclaration, isConnection: true);

    public static ConnectionClassInfo CreateEdge(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax classDeclaration)
        => Create(compilation, runtimeType, classDeclaration, isConnection: false);

    private static ConnectionClassInfo Create(
        Compilation compilation,
        INamedTypeSymbol runtimeType,
        ClassDeclarationSyntax classDeclaration,
        bool isConnection)
    {
        var name = runtimeType.Name + "Type";

        var resolvers = ImmutableArray.CreateBuilder<Resolver>();

        foreach (var member in runtimeType.AllPublicInstanceMembers())
        {
            switch (member)
            {
                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                    resolvers.Add(ObjectTypeInspector.CreateResolver(compilation, runtimeType, method, name));
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
                            ObjectTypeInspector.GetMemberBindings(member),
                            flags: flags));
                    break;
            }
        }

        return new ConnectionClassInfo(
            runtimeType,
            classDeclaration,
            resolvers.ToImmutable());
    }
}
