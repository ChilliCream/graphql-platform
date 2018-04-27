using System;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchemaConfiguration
    {
        ISchemaConfiguration Resolver(string typeName, string fieldName, FieldResolverDelegate fieldResolver);
        ISchemaConfiguration Resolver<TResolver, TObjectType>();

        ISchemaConfiguration Name<TObjectType>(string typeName);
        ISchemaConfiguration Name<TObjectType>(string typeName,
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);
        ISchemaConfiguration Name<TObjectType>(
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);

        ISchemaConfiguration Register<T>(T type)
            where T : INamedType;
    }
}
