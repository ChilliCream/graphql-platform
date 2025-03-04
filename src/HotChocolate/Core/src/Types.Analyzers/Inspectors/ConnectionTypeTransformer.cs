using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class ConnectionTypeTransformer : IPostCollectSyntaxTransformer
{
    public ImmutableArray<SyntaxInfo> Transform(
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        List<Resolver>? connectionResolvers = null;
        Dictionary<string, ConnectionClassInfo>? connectionClassLookup = null;

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is ConnectionClassInfo connectionClass)
            {
                connectionClassLookup ??= [];
                connectionClassLookup[connectionClass.RuntimeType.ToFullyQualified()] = connectionClass;
                continue;
            }

            if(syntaxInfo is IOutputTypeInfo typeInfo
                && typeInfo.Resolvers.Any(t => t.Kind is ResolverKind.ConnectionResolver))
            {
                foreach (var resolver in typeInfo.Resolvers)
                {
                    if(resolver.Kind is ResolverKind.ConnectionResolver)
                    {
                        connectionResolvers ??= [];
                        connectionResolvers.Add(resolver);
                    }
                }
            }
        }

        if (connectionResolvers is not null)
        {
            connectionClassLookup ??= [];
            var connectionTypeLookup = new Dictionary<string, IOutputTypeInfo>();
            List<ConnectionObjectTypeInfo>? connectionTypeInfos = null;

            foreach (var syntaxInfo in syntaxInfos)
            {
                if(syntaxInfo is IOutputTypeInfo { HasRuntimeType: true } typeInfo)
                {
                    connectionTypeLookup[typeInfo.RuntimeTypeFullName] = typeInfo;
                }
            }

            foreach (var connectionResolver in connectionResolvers)
            {
                var connectionType = GetConnectionType(compilation, connectionResolver.Member.GetReturnType());
                var connectionTypeName = connectionType.ToFullyQualified();
                if (connectionTypeLookup.ContainsKey(connectionTypeName))
                {
                    continue;
                }

                var edgeType = GetEdgeType(connectionType);
                if (edgeType is null)
                {
                    continue;
                }

                var connectionTypeInfo =
                    connectionClassLookup.TryGetValue(connectionTypeName, out var connectionClass)
                        ? ConnectionObjectTypeInfo.CreateConnectionFrom(connectionClass)
                        : ConnectionObjectTypeInfo.CreateConnection(compilation, connectionType, null);

                connectionTypeInfos ??= [];
                connectionTypeInfos.Add(connectionTypeInfo);
                connectionTypeLookup.Add(connectionType.ToFullyQualified(), connectionTypeInfo);

                if (connectionTypeLookup.ContainsKey(edgeType.ToFullyQualified()))
                {
                    continue;
                }

                var edgeTypeInfo = ConnectionObjectTypeInfo.CreateEdge(compilation, edgeType, null);
                connectionTypeInfos.Add(edgeTypeInfo);
                connectionTypeLookup.Add(edgeType.ToFullyQualified(), edgeTypeInfo);
            }

            if (connectionTypeInfos is not null)
            {
                return syntaxInfos.AddRange(connectionTypeInfos);
            }
        }

        return syntaxInfos;
    }

    private static INamedTypeSymbol GetConnectionType(Compilation compilation, ITypeSymbol? possibleConnectionType)
    {
        if(possibleConnectionType is null)
        {
            Throw();
        }

        if(compilation.IsTaskOrValueTask(possibleConnectionType, out var type))
        {
            if(type is INamedTypeSymbol namedType1)
            {
                return namedType1;
            }

            Throw();
        }

        if (possibleConnectionType is not INamedTypeSymbol namedType2)
        {
            Throw();
        }

        return namedType2;

        [DoesNotReturn]
        static void Throw() => throw new InvalidOperationException("Could not resolve connection base type.");
    }

    private static INamedTypeSymbol? GetEdgeType(INamedTypeSymbol connectionType)
    {
        var property = connectionType.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == "Edges");

        if (property is null)
        {
            return null;
        }

        var returnType = property.GetReturnType();
        if (returnType is not INamedTypeSymbol namedType
            || !namedType.IsGenericType
            || namedType.TypeArguments.Length != 1
            || namedType.Name != "IReadOnlyList")
        {
            return null;
        }

        return (INamedTypeSymbol)namedType.TypeArguments[0];
    }
}
