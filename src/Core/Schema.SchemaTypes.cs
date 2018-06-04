using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate
{
    public partial class Schema
    {
        private sealed class SchemaTypes
        {
            public Dictionary<string, INamedType> _types;

            private SchemaTypes(
                IEnumerable<INamedType> types,
                string queryTypeName,
                string mutationTypeName,
                string subscriptionTypeName)
            {
                _types = types.ToDictionary(t => t.Name);

                INamedType namedType;
                if (_types.TryGetValue(queryTypeName, out namedType)
                    && namedType is ObjectType queryType)
                {
                    QueryType = queryType;
                }

                if (_types.TryGetValue(mutationTypeName, out namedType)
                   && namedType is ObjectType mutationType)
                {
                    MutationType = mutationType;
                }

                if (_types.TryGetValue(subscriptionTypeName, out namedType)
                   && namedType is ObjectType subscriptionType)
                {
                    SubscriptionType = subscriptionType;
                }
            }

            public ObjectType QueryType { get; }
            public ObjectType MutationType { get; }
            public ObjectType SubscriptionType { get; }

            public T GetType<T>(string typeName) where T : IType
            {
                if (_types.TryGetValue(typeName, out INamedType namedType)
                    && namedType is T type)
                {
                    return type;
                }

                throw new ArgumentException(
                    "The specified type does not exist or " +
                    "is not of the specified kind.",
                    nameof(typeName));
            }

            public bool TryGetType<T>(string typeName, out T type) where T : IType
            {
                if (_types.TryGetValue(typeName, out INamedType namedType)
                    && namedType is T t)
                {
                    type = t;
                    return true;
                }

                type = default(T);
                return false;
            }

            public IReadOnlyCollection<INamedType> GetTypes()
            {
                return _types.Values;
            }

            public static SchemaTypes Create(
                IEnumerable<INamedType> types,
                SchemaNames names)
            {
                if (types == null)
                {
                    throw new ArgumentNullException(nameof(types));
                }

                SchemaNames n = string.IsNullOrEmpty(names.QueryTypeName)
                    ? new SchemaNames(null, null, null)
                    : names;

                return new SchemaTypes(types,
                    n.QueryTypeName,
                    n.MutationTypeName,
                    n.SubscriptionTypeName);
            }
        }
    }
}
