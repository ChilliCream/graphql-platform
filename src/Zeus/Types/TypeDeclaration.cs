using System;

namespace Zeus.Types
{
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

        public override string ToString()
        {
            string stringRepresentation;
            if (Kind == TypeKind.List)
            {
                stringRepresentation = $"[{ElementType.ToString()}]";
            }
            else
            {
                stringRepresentation = Name;
            }

            if (IsNullable)
            {
                return stringRepresentation;
            }
            else
            {
                return $"{stringRepresentation}!";
            }
        }
    }
}