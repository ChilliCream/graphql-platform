using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class SchemaTypes
    {
        private readonly Dictionary<NameString, INamedType> _types;
        private readonly Dictionary<NameString, ITypeBinding> _typeBindings;
        private readonly Dictionary<NameString, ImmutableList<ObjectType>> _possibleTypes;

        private SchemaTypes(
            IEnumerable<INamedType> types,
            IEnumerable<ITypeBinding> typeBindings,
            string queryTypeName,
            string mutationTypeName,
            string subscriptionTypeName)
        {
            _types = types.ToDictionary(t => t.Name);
            _typeBindings = typeBindings.ToDictionary(t => t.Name);
            _possibleTypes = CreatePossibleTypeLookup(_types.Values);

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

        public T GetType<T>(NameString typeName) where T : IType
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

        public bool TryGetType<T>(NameString typeName, out T type)
            where T : IType
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

        public bool TryGetClrType(NameString typeName, out Type clrType)
        {
            if (_typeBindings.TryGetValue(typeName, out ITypeBinding binding))
            {
                if (binding is ObjectTypeBinding otb)
                {
                    clrType = otb.Type;
                    return true;
                }

                if (binding is InputObjectTypeBinding iotb)
                {
                    clrType = iotb.Type;
                    return true;
                }
            }

            clrType = null;
            return false;
        }

        public bool TryGetPossibleTypes(
            string abstractTypeName,
            out ImmutableList<ObjectType> types)
        {
            return _possibleTypes.TryGetValue(abstractTypeName, out types);
        }

        private static Dictionary<NameString, ImmutableList<ObjectType>> CreatePossibleTypeLookup(
            IReadOnlyCollection<INamedType> types)
        {
            Dictionary<NameString, List<ObjectType>> possibleTypes =
                new Dictionary<NameString, List<ObjectType>>();

            foreach (ObjectType objectType in types.OfType<ObjectType>())
            {
                foreach (InterfaceType interfaceType in
                    objectType.Interfaces.Values)
                {
                    if (!possibleTypes.TryGetValue(
                        interfaceType.Name, out List<ObjectType> pt))
                    {
                        pt = new List<ObjectType>();
                        possibleTypes[interfaceType.Name] = pt;
                    }

                    pt.Add(objectType);
                }
            }

            foreach (UnionType unionType in types.OfType<UnionType>())
            {
                foreach (ObjectType objectType in unionType.Types.Values)
                {
                    if (!possibleTypes.TryGetValue(
                        unionType.Name, out List<ObjectType> pt))
                    {
                        pt = new List<ObjectType>();
                        possibleTypes[unionType.Name] = pt;
                    }

                    pt.Add(objectType);
                }
            }

            return possibleTypes.ToDictionary(
                t => t.Key, t => t.Value.ToImmutableList());
        }

        public static SchemaTypes Create(
            IEnumerable<INamedType> types,
            IEnumerable<ITypeBinding> typeBindings,
            IReadOnlySchemaOptions options)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (typeBindings == null)
            {
                throw new ArgumentNullException(nameof(typeBindings));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new SchemaTypes(types,
                typeBindings,
                options.QueryTypeName,
                options.MutationTypeName,
                options.SubscriptionTypeName);
        }
    }
}
