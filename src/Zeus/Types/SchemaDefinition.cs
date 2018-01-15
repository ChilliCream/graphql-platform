using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Zeus.Types
{
    public enum TypeKind
    {
        Object,
        Input,
        Scalar,
        List
    }

    public class TypeDeclaration
    {
        public TypeDeclaration(string name, bool isNullable, TypeKind kind)
            : this(name, isNullable, kind, null)
        {
        }

        public TypeDeclaration(string name, bool isNullable, TypeKind kind, TypeDeclaration elementType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsNullable = isNullable;
            Kind = kind;
            ElementType = elementType;
        }

        public string Name { get; }
        public bool IsNullable { get; }
        public TypeKind Kind { get; }
        public TypeDeclaration ElementType { get; }
    }

    public class ArgumentDeclaration
    {
        public ArgumentDeclaration(string name, TypeDeclaration type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public string Name { get; }
        public TypeDeclaration Type { get; }
    }

    public class FieldDeclaration
    {
        public FieldDeclaration(string name, IEnumerable<ArgumentDeclaration> arguments)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (arguments == null)
            {
                throw new System.ArgumentNullException(nameof(arguments));
            }

            Name = Name;
            Arguments = arguments.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public string Name { get; }
        public TypeDeclaration Type { get; }
        public IReadOnlyDictionary<string, ArgumentDeclaration> Arguments { get; }
    }

    public class ObjectDeclarationBase
    {
        protected ObjectDeclarationBase(string name, IEnumerable<FieldDeclaration> fields)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (fields == null)
            {
                throw new System.ArgumentNullException(nameof(fields));
            }

            Name = Name;
            Fields = fields.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, FieldDeclaration> Fields { get; }
    }

    public class ObjectDeclaration
        : ObjectDeclarationBase
    {
        public ObjectDeclaration(string name, IEnumerable<FieldDeclaration> fields)
            : base(name, fields)
        {
        }
    }

    public class InputDeclaration
        : ObjectDeclarationBase
    {
        public InputDeclaration(string name, IEnumerable<FieldDeclaration> fields)
            : base(name, fields)
        {
        }
    }
}