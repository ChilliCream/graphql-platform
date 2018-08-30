using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Configuration
{
    internal interface IResolverRegistry
    {
        void RegisterResolver(IFieldReference resolverBinding);

        void RegisterResolver(IFieldResolverDescriptor resolverDescriptor);

        bool ContainsResolver(FieldReference fieldReference);

        FieldResolverDelegate GetResolver(string typeName, string fieldName);
    }
}
