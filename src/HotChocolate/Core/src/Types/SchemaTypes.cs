using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class SchemaTypes
    {
        private readonly Dictionary<NameString, INamedType> _types;
        private readonly Dictionary<NameString, List<ObjectType>> _possibleTypes;

        public SchemaTypes(SchemaTypesDefinition definition)
        {
            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            _types = definition.Types.ToDictionary(t => t.Name);
            _possibleTypes = CreatePossibleTypeLookup(definition.Types);
            QueryType = definition.QueryType;
            MutationType = definition.MutationType;
            SubscriptionType = definition.SubscriptionType;
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

            // TODO : resource
            throw new ArgumentException(
                $"The specified type `{typeName}` does not exist or " +
                $"is not of the specified kind `{typeof(T).Name}`.",
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

            type = default;
            return false;
        }

        public IReadOnlyCollection<INamedType> GetTypes()
        {
            return _types.Values;
        }

        public bool TryGetClrType(NameString typeName, out Type clrType)
        {
            if (_types.TryGetValue(typeName, out INamedType type)
                && type is IHasRuntimeType ct
                && ct.RuntimeType != typeof(object))
            {
                clrType = ct.RuntimeType;
                return true;
            }

            clrType = null;
            return false;
        }

        public bool TryGetPossibleTypes(
            string abstractTypeName,
            out IReadOnlyList<ObjectType> types)
        {
            if (_possibleTypes.TryGetValue(abstractTypeName, out List<ObjectType> pt))
            {
                types = pt;
                return true;
            }

            types = null;
            return false;
        }

        private static Dictionary<NameString, List<ObjectType>> CreatePossibleTypeLookup(
            IReadOnlyCollection<INamedType> types)
        {
            var possibleTypes = new Dictionary<NameString, List<ObjectType>>();

            foreach (ObjectType objectType in types.OfType<ObjectType>())
            {
                possibleTypes[objectType.Name] = new List<ObjectType> { objectType };

                foreach (InterfaceType interfaceType in objectType.Interfaces)
                {
                    if (!possibleTypes.TryGetValue(interfaceType.Name, out List<ObjectType> pt))
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

            return possibleTypes;
        }
    }
}
