using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Configuration
{
    internal interface IResolverRegistry
    {
        void RegisterResolver(IFieldReference resolverBinding);

        void RegisterMiddleware(IDirectiveMiddleware middleware);

        void RegisterResolver(IFieldResolverDescriptor resolverDescriptor);

        bool ContainsResolver(FieldReference fieldReference);

        AsyncFieldResolverDelegate GetResolver(
            string typeName,
            string fieldName);

        IDirectiveMiddleware GetMiddleware(string directiveName);
    }
}
