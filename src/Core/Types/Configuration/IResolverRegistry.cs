using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Configuration
{
    internal interface IResolverRegistry
    {
        void RegisterResolver(IFieldReference resolverBinding);

        void RegisterMiddleware(IDirectiveMiddleware middleware);

        void RegisterMiddleware(FieldMiddleware middleware);

        void RegisterResolver(IFieldResolverDescriptor resolverDescriptor);

        bool ContainsResolver(FieldReference fieldReference);

        FieldResolverDelegate GetResolver(
            NameString typeName,
            NameString fieldName);

        IDirectiveMiddleware GetMiddleware(string directiveName);

        FieldResolverDelegate CreateMiddleware(
            FieldResolverDelegate fieldResolver);
    }
}
