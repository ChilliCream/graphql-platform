using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    internal interface ISchemaContext
    {
        IsOfType CreateIsOfType(string typeName);
        AsyncFieldResolverDelegate CreateResolver(string typeName, string fieldName);
        ResolveType CreateTypeResolver(string typeName);
        IReadOnlyCollection<INamedType> GetAllTypes();
        IInputType GetInputType(string typeName);
        IOutputType GetOutputType(string typeName);
        T GetOutputType<T>(string typeName) where T : IOutputType;
        INamedType GetType(string typeName);
        T GetType<T>(string typeName) where T : INamedType;
        void RegisterResolvers(IEnumerable<FieldResolver> fieldResolvers);
        void RegisterType(INamedType type);
        void RegisterTypeMappings(IEnumerable<KeyValuePair<string, Type>> typeMappings);
        bool TryGetOutputType<T>(string typeName, out T type) where T : IOutputType;
    }
}