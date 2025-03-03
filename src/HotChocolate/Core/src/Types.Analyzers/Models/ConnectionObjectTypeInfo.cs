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
    private readonly INamedTypeSymbol _runtimeType;

    public ConnectionObjectTypeInfo(Compilation compilation, INamedTypeSymbol runtimeType, bool isConnection)
    {
        _runtimeType = runtimeType;
        ClassName = runtimeType.Name + "Type";

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

                    resolvers.Add(CreateResolver(compilation, _runtimeType, method, ClassName));
                    break;

                case IPropertySymbol property:
                    var flags = FieldFlags.None;

                    if (isConnection)
                    {
                        if(property.Name.Equals("Edges", StringComparison.Ordinal))
                        {
                            flags |= FieldFlags.ConnectionEdgesField;
                        }
                        else if(property.Name.Equals("Nodes", StringComparison.Ordinal))
                        {
                            flags |= FieldFlags.ConnectionNodesField;
                        }
                        else if(property.Name.Equals("TotalCount", StringComparison.Ordinal))
                        {
                            flags |= FieldFlags.TotalCount;
                        }
                    }

                    resolvers.Add(
                        new Resolver(
                            ClassName,
                            property,
                            ResolverResultKind.Pure,
                            ImmutableArray<ResolverParameter>.Empty,
                            GetMemberBindings(member),
                            flags: flags));
                    break;
            }
        }

        Resolvers = resolvers.ToImmutable();
    }

    public string Name => _runtimeType.ToFullyQualified();

    public bool IsRootType => false;

    public INamedTypeSymbol? Type => null;

    public INamedTypeSymbol RuntimeType => _runtimeType;

    public string ClassName { get; }

    public string Namespace => _runtimeType.ContainingNamespace.ToDisplayString();

    public ClassDeclarationSyntax? ClassDeclarationSyntax => null;

    public ImmutableArray<Resolver> Resolvers { get; }

    public override string OrderByKey => Name;

    public override bool Equals(object? obj)
        => obj is ConnectionObjectTypeInfo other && Equals(other);

    public override bool Equals(SyntaxInfo obj)
        => obj is ConnectionObjectTypeInfo other && Equals(other);

    private bool Equals(ConnectionObjectTypeInfo other)
        => string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(Name);
}
