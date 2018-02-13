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
            _fields = fields.ToArray();
            Interfaces = interfaces.ToArray();
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

        public static __Type CreateInterfaceType(string name, string description, IEnumerable<__Field> fields)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (fields == null)
            {
                throw new System.ArgumentNullException(nameof(fields));
            }

            return new __Type(__TypeKind.Interface, name, description, fields, null);
        }
    }

    internal class __Field
    {
        private __Field(string name, string description, IEnumerable<__InputValue> arguments, 
            IType type, bool isDepricated, string depricationReason)
        {
            Name = name;
            Description = description;
            Arguments = arguments.ToArray();
            Type = type;
            IsDepricated = isDepricated;
            DepricationReason = depricationReason;
        }

        public string Name { get; }
        public string Description { get; }

        [GraphQLName("args")]
        public IReadOnlyCollection<__InputValue> Arguments { get; }

        public IType Type { get; }

        public bool IsDepricated { get; }

        public string DepricationReason { get; }
    }

    internal class __InputValue
    {
        private __InputValue(string name, string description, IType type, string defaultValue)
        {
            Name = name;
            Description = description;
            Type = type;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public string Description { get; }
        public IType Type { get; }
        public string DefaultValue { get; }

        public static __InputValue Create(string name, string description, IType type, string defaultValue)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new System.ArgumentNullException(nameof(type));
            }

            return new __InputValue(name, description, type, defaultValue);
        }
    }

    internal class __Schema
    {

    }
}