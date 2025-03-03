using System.Collections.Immutable;
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

        foreach (var syntaxInfo in syntaxInfos)
        {
            if(syntaxInfo is not IOutputTypeInfo typeInfo
                || typeInfo.Resolvers.All(t => t.Kind is not ResolverKind.ConnectionResolver))
            {
                continue;
            }

            foreach (var resolver in typeInfo.Resolvers)
            {
                if(resolver.Kind is ResolverKind.ConnectionResolver)
                {
                    connectionResolvers ??= [];
                    connectionResolvers.Add(resolver);
                }
            }
        }

        if (connectionResolvers is not null)
        {
            var connectionTypeLookup = new Dictionary<string, IOutputTypeInfo>();
            List<ConnectionObjectTypeInfo>? connectionTypeInfos = null;

            foreach (var syntaxInfo in syntaxInfos)
            {
                if(syntaxInfo is IOutputTypeInfo { RuntimeType: not null } typeInfo)
                {
                    connectionTypeLookup[typeInfo.RuntimeType.ToFullyQualified()] = typeInfo;
                }
            }

            foreach (var connectionResolver in connectionResolvers)
            {
                var connectionType = GetConnectionType(connectionResolver);
                if (connectionTypeLookup.ContainsKey(connectionType.ToFullyQualified()))
                {
                    continue;
                }

                var edgeType = GetEdgeType(connectionType);
                if (edgeType is null)
                {
                    continue;
                }

                var connectionTypeInfo = new ConnectionObjectTypeInfo(compilation, connectionType, isConnection: true);
                connectionTypeInfos ??= [];
                connectionTypeInfos.Add(connectionTypeInfo);
                connectionTypeLookup.Add(connectionType.ToFullyQualified(), connectionTypeInfo);

                if (connectionTypeLookup.ContainsKey(edgeType.ToFullyQualified()))
                {
                    continue;
                }

                var edgeTypeInfo = new ConnectionObjectTypeInfo(compilation, connectionType, isConnection: true);
                connectionTypeInfos.Add(edgeTypeInfo);
                connectionTypeLookup.Add(edgeType.ToFullyQualified(), edgeTypeInfo);
            }

            if(connectionTypeInfos is not null)
            {
                return syntaxInfos.AddRange(connectionTypeInfos);
            }
        }

        return syntaxInfos;

        static INamedTypeSymbol GetConnectionType(Resolver connectionResolver)
            => (INamedTypeSymbol)connectionResolver.Member.GetReturnType()!.UnwrapTaskOrValueTask();

        static INamedTypeSymbol? GetEdgeType(INamedTypeSymbol connectionType)
            => connectionType.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == "Edge")
                ?.Type as INamedTypeSymbol;
    }
}
