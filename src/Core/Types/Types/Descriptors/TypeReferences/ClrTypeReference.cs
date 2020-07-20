using System;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors
{
    public sealed class ClrTypeReference
        : TypeReferenceBase
        , IClrTypeReference
        , IEquatable<ClrTypeReference>
        , IEquatable<IClrTypeReference>
    {
        public ClrTypeReference(
            Type type, TypeContext context)
            : this(type, context, null, null)
        {
        }

        public ClrTypeReference(
            Type type, TypeContext context,
            bool? isTypeNullable = null, bool? isElementTypeNullable = null)
            : base(context, isTypeNullable, isElementTypeNullable)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
        }

        public Type Type { get; }

        public IClrTypeReference Compile()
        {
            if (IsTypeNullable.HasValue || IsElementTypeNullable.HasValue)
            {
                var nullable = new Utilities.Nullable[2];


                nullable[0] = IsTypeNullable.HasValue
                    ? (IsTypeNullable.Value ? Utilities.Nullable.Yes : Utilities.Nullable.No)
                    : Utilities.Nullable.Undefined;

                nullable[1] = IsElementTypeNullable.HasValue 
                    ? (IsElementTypeNullable.Value ? Utilities.Nullable.Yes : Utilities.Nullable.No)
                    : Utilities.Nullable.Undefined;


                ExtendedType extendedType = ExtendedType.FromType(Type);
                return new ClrTypeReference(
                    ExtendedTypeRewriter.Rewrite(extendedType, nullable),
                    Context);
            }
            return this;
        }

        public bool Equals(ClrTypeReference other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Context != other.Context
                && Context != TypeContext.None
                && other.Context != TypeContext.None)
            {
                return false;
            }

            return Type.Equals(other.Type)
                && IsTypeNullable.Equals(other.IsTypeNullable)
                && IsElementTypeNullable.Equals(other.IsElementTypeNullable);
        }

        public bool Equals(IClrTypeReference other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Context != other.Context
                && Context != TypeContext.None
                && other.Context != TypeContext.None)
            {
                return false;
            }

            return Type.Equals(other.Type)
                && IsTypeNullable.Equals(other.IsTypeNullable)
                && IsElementTypeNullable.Equals(other.IsElementTypeNullable);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is ClrTypeReference c)
            {
                return Equals(c);
            }

            if (obj is IClrTypeReference ic)
            {
                return Equals(ic);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Type.GetHashCode() * 397;
                hash = hash ^ (IsTypeNullable?.GetHashCode() ?? 0 * 11);
                hash = hash ^ (IsElementTypeNullable?.GetHashCode() ?? 0 * 13);
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Context}: {Type.GetTypeName()}";
        }

        public IClrTypeReference WithoutContext()
        {
            return new ClrTypeReference(
                Type, TypeContext.None,
                IsTypeNullable, IsElementTypeNullable);
        }

        public IClrTypeReference WithType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new ClrTypeReference(
                type,
                Context,
                IsTypeNullable,
                IsElementTypeNullable);
        }

        public static ClrTypeReference FromSchemaType<T>()
            where T : ITypeSystemMember
        {
            return new ClrTypeReference(
                typeof(T),
                SchemaTypeReference.InferTypeContext(typeof(T)));
        }

        public static ClrTypeReference FromSchemaType(Type schemaType)
        {
            if (schemaType == null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            if (typeof(ITypeSystemMember).IsAssignableFrom(schemaType))
            {

                return new ClrTypeReference(
                    schemaType,
                    SchemaTypeReference.InferTypeContext(schemaType));
            }

            throw new ArgumentException(
                TypeResources.ClrTypeReference_OnlyTsosAreAllowed);
        }
    }
}
