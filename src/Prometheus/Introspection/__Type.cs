using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus.Abstractions;

namespace Prometheus.Introspection
{
    internal partial class __Type
    {
        private readonly __TypeKind _kind;
        private readonly __Field[] _fields;
        private readonly NamedType[] _interfaces;

        private __Type(__TypeKind kind, string name, string description,
            IEnumerable<__Field> fields, IEnumerable<NamedType> interfaces)
        {
            _kind = kind;
            Name = name;
            Description = description;
            _fields = fields == null ? null : fields.ToArray();
            _interfaces = interfaces == null ? null : interfaces.ToArray();
        }

        private __Type(string name, string description,
           IEnumerable<__InputValue> inputFields)
        {
            _kind = __TypeKind.InputObject;
            Name = name;
            Description = description;
            InputFields = inputFields.ToArray();
        }

        private __Type(__TypeKind kind, __Type ofType)
        {
            _kind = kind;
            OfType = ofType;
        }

        public string Name { get; }
        public string Description { get; }
        public IReadOnlyCollection<__InputValue> InputFields { get; }
        public __Type OfType { get; }

        [GraphQLName("kind")]
        public string GetKind()
        {
            return _kind.ToString();
        }

        // object and interfaces only
        [GraphQLName("fields")]
        public IEnumerable<__Field> GetFields(bool includeDeprecated)
        {
            if (_kind != __TypeKind.Interface
                && _kind != __TypeKind.Object)
            {
                return null;
            }

            if (includeDeprecated)
            {
                return _fields;
            }
            return _fields.Where(t => t.IsDeprecated == false);
        }

        // object only
        [GraphQLName("interfaces")]
        public IEnumerable<__Type> GetInterfaces(ISchema schema)
        {
            if (_kind == __TypeKind.Object)
            {
                return GetInterfacesInternal();
            }
            return null;

            IEnumerable<__Type> GetInterfacesInternal()
            {
                foreach (NamedType interfaceType in _interfaces)
                {
                    if (schema.InterfaceTypes.TryGetValue(interfaceType.Name,
                        out var interfaceDefinition))
                    {
                        yield return CreateInterfaceType(interfaceDefinition);
                    }
                }
            }
        }

        // interface and Union only
        [GraphQLName("possibleTypes")]
        public IEnumerable<__Type> GetPossibleTypes(ISchema schema)
        {
            if (_kind == __TypeKind.Interface)
            {
                return GetImplementingTypes(schema);
            }

            if (_kind == __TypeKind.Union)
            {
                return GetUnionTypes(schema);
            }

            return null;
        }

        private IEnumerable<__Type> GetImplementingTypes(ISchema schema)
        {
            foreach (ObjectTypeDefinition objectType in schema.ObjectTypes
                .Values.Where(t => t.Interfaces.Contains(Name)))
            {
                yield return CreateObjectType(objectType);
            }
        }

        private IEnumerable<__Type> GetUnionTypes(ISchema schema)
        {
            foreach (NamedType namedType in schema.UnionTypes[Name].Types)
            {
                if (schema.ObjectTypes.TryGetValue(namedType.Name,
                    out var objectTypeDefinition))
                {
                    yield return CreateType(objectTypeDefinition);
                }
            }
        }

        [GraphQLName("enumValues")]
        public IEnumerable<__EnumValue> GetEnumValues(ISchema schema, bool includeDeprecated)
        {
            if (_kind == __TypeKind.Enum)
            {
                EnumTypeDefinition enumType = schema.EnumTypes[Name];
                return enumType.Values.Select(t => new __EnumValue(t, null, false, null));
            }
            return null;
        }
    }
}