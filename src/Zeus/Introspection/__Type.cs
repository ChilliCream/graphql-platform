using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;

namespace Zeus.Introspection
{
    internal class __Type
    {
        private readonly __Field[] _fields;

        private __Type(__TypeKind kind, string name, string description, IEnumerable<__Field> fields, IEnumerable<NamedType> interfaces)
        {
            Kind = kind;
            Name = name;
            Description = description;
            _fields = fields == null ? null : fields.ToArray();
            Interfaces = interfaces == null ? null : interfaces.ToArray();
        }

        public __TypeKind Kind { get; }
        public string Name { get; }
        public string Description { get; }

        // object and interfaces only
        [GraphQLName("fields")]
        public IEnumerable<__Field> GetFields(bool includeDepricated)
        {
            if (Kind != __TypeKind.Interface
                && Kind != __TypeKind.Object)
            {
                return null;
            }

            if (includeDepricated)
            {
                return _fields;
            }
            return _fields.Where(t => t.IsDepricated == false);
        }

        // object only
        public IReadOnlyCollection<NamedType> Interfaces { get; } // => add resolver that looks up the __Types ...

        // interface and Union only
        public IReadOnlyCollection<NamedType> PossibleTypes { get; } // => add resolver that looks up the __Types ...

        public static __Type CreateType(ITypeDefinition typeDefinition)
        {
            if (typeDefinition is InterfaceTypeDefinition itd)
            {
                return CreateInterfaceType(itd);
            }
            return null;
        }

        public static __Type CreateObjectType(string name, string description, IEnumerable<__Field> fields, IEnumerable<NamedType> interfaces)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (fields == null)
            {
                throw new System.ArgumentNullException(nameof(fields));
            }

            if (interfaces == null)
            {
                throw new System.ArgumentNullException(nameof(interfaces));
            }

            return new __Type(__TypeKind.Object, name, description, fields, interfaces);
        }

        private static __Type CreateInterfaceType(InterfaceTypeDefinition interfaceType)
        {
            if (interfaceType == null)
            {
                throw new System.ArgumentNullException(nameof(interfaceType));
            }

            return new __Type(__TypeKind.Interface,
                interfaceType.Name, null,
                CreateFields(interfaceType.Fields.Values),
                null);
        }

        private static IEnumerable<__Field> CreateFields(IEnumerable<FieldDefinition> fields)
        {
            yield break;
        }
    }
}