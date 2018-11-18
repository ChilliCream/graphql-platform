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
            NameString typeName,
            NameString fieldName);

        IDirectiveMiddleware GetMiddleware(string directiveName);
    }
}
