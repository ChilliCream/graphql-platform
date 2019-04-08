using System;
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
            bool? isTypeNullable, bool? isElementTypeNullable)
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
                Type rewritten = DotNetTypeInfoFactory.Rewrite(
                    Type,
                    !(IsTypeNullable ?? false),
                    !(IsElementTypeNullable ?? false));
                return new ClrTypeReference(rewritten, Context);
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
                hash = hash ^ (Context.GetHashCode() * 7);
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

        public static ClrTypeReference FromSchemaType<T>()
            where T : ITypeSystem
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

            if (typeof(ITypeSystem).IsAssignableFrom(schemaType))
            {

                return new ClrTypeReference(
                    schemaType,
                    SchemaTypeReference.InferTypeContext(schemaType));
            }

            // TODO : resources
            throw new ArgumentException(
                "Only type system objects are allowed.");
        }
    }
}
