using System;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public interface ISchemaConfiguration
    {
        ISchemaConfiguration Resolver(string typeName, string fieldName, FieldResolverDelegate fieldResolver);
        ISchemaConfiguration Resolver<TResolver>();
        ISchemaConfiguration Resolver<TResolver>(string typeName);
        ISchemaConfiguration Resolver<TResolver, TObjectType>();
        ISchemaConfiguration Resolver<TResolver, TObjectType>(string typeName);

        ISchemaConfiguration Name<TObjectType>(string typeName);
        ISchemaConfiguration Name<TObjectType>(string typeName,
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);
        ISchemaConfiguration Name<TObjectType>(
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping);

    }
}