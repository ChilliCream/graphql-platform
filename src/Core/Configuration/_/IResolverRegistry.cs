using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    internal interface IResolverRegistry
    {
        void RegisterResolver(ResolverBinding resolverBinding);
        void RegisterResolver(FieldResolverDescriptor resolverDescriptor);
        bool ContainsResolver(FieldReference fieldReference);
        FieldResolverDelegate GetResolver(string typeName, string fieldName);
    }
}
