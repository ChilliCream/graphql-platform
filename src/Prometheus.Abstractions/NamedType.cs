using System;

namespace Prometheus.Abstractions
{
    public sealed class NamedType
        : IType
        , IEquatable<IType>
        , IEquatable<NamedType>
    {
        public NamedType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("The name must not be null.", nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public bool Equals(NamedType other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Name.Equals(Name, StringComparison.Ordinal);
        }

        public bool Equals(IType other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return Equals(other as NamedType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as NamedType);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 397 ^ (Name.GetHashCode() * 7);
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public static NamedType String { get; } = new NamedType(ScalarTypes.String);

        public static NonNullType NonNullString { get; } = new NonNullType(String);

        public static NamedType Integer { get; } = new NamedType(ScalarTypes.Integer);

        public static NonNullType NonNullInteger{ get; } = new NonNullType(Integer);

        public static NamedType Float { get; } = new NamedType(ScalarTypes.Float);

        public static NonNullType NonNullFloat { get; } = new NonNullType(Float);

        public static NamedType ID { get; } = new NamedType(ScalarTypes.ID);

        public static NonNullType NonNullID { get; } = new NonNullType(ID);
        
        public static NamedType Boolean { get; } = new NamedType(ScalarTypes.Boolean);

        public static NonNullType NonNullBoolean{ get; } = new NonNullType(Boolean);
    }
}