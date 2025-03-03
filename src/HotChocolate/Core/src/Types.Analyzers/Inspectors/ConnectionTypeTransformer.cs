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
            var connectionTypeLookup = new Dictionary<ITypeSymbol, IOutputTypeInfo>(SymbolEqualityComparer.Default);
            List<ConnectionObjectTypeInfo>? connectionTypeInfos = null;

            foreach (var syntaxInfo in syntaxInfos)
            {
                if(syntaxInfo is IOutputTypeInfo { RuntimeType: not null } typeInfo)
                {
                    connectionTypeLookup[typeInfo.RuntimeType] = typeInfo;
                }
            }

            foreach (var connectionResolver in connectionResolvers)
            {
                var connectionType = connectionResolver.Member.GetReturnType()!.UnwrapTaskOrValueTask();

                if (connectionTypeLookup.ContainsKey(connectionType))
                {
                    continue;
                }

                var connectionTypeInfo = new ConnectionObjectTypeInfo(
                    compilation,
                    (INamedTypeSymbol)connectionType);

                connectionTypeInfos ??= [];
                connectionTypeInfos.Add(connectionTypeInfo);
                connectionTypeLookup.Add(connectionType, connectionTypeInfo);
            }

            if(connectionTypeInfos is not null)
            {
                return syntaxInfos.AddRange(connectionTypeInfos);
            }
        }

        return syntaxInfos;
    }
}
