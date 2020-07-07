using System;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    /// <summary>
    /// A type reference is used to refer to a type in the type system. 
    /// This allows us to loosly couple types.
    /// </summary>
    public abstract class TypeReference
        : ITypeReference
    {
        protected TypeReference(
            TypeContext context,
            string? scope,
            bool[]? nullable)
        {
            Context = context;
        }

        /// <summary>
        /// The context in which the type reference was created.
        /// </summary>
        public TypeContext Context { get; }

        /// <summary>
        /// The scope in which the type reference was created.
        /// </summary>
        /// <value></value>
        public string? Scope { get; }

        /// <summary>
        /// Gets nullability hints.
        /// </summary>
        public bool[]? Nullable { get; }

        protected bool IsEqual(ITypeReference other)
        {
            if (Context != other.Context
                && Context != TypeContext.None
                && other.Context != TypeContext.None)
            {
                return false;
            }

            if (!Scope.EqualsOrdinal(other.Scope))
            {
                return false;
            }

            if (Nullable is null)
            {
                return other.Nullable is null;
            }

            if (other.Nullable is null)
            {
                return false;
            }

            if (Nullable.Length != other.Nullable.Length)
            {
                return false;
            }

            for (int i = 0; i < Nullable.Length; i++)
            {
                if (Nullable[i] != other.Nullable[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Context.GetHashCode() * 397;

                if (Scope is { })
                {
                    hash ^= Scope.GetHashCode() * 397;
                }

                if (Nullable is { })
                {
                    hash ^= 397;
                    for (int i = 0; i < Nullable.Length; i++)
                    {
                        hash ^= Nullable[i].GetHashCode() * 397;
                    }
                }

                return hash;
            }
        }

        public static ClrTypeReference FromType<T>(
            TypeContext? context = null,
            string? scope = null,
            bool[]? nullable = null)
            where T : ITypeSystemMember =>
            FromType(typeof(T));

        public static ClrTypeReference FromType(
            Type type,
            TypeContext? context = null,
            string? scope = null,
            bool[]? nullable = null)
        {
            if (typeof(ITypeSystemMember).IsAssignableFrom(type))
            {
                return new ClrTypeReference(
                    type,
                    SchemaTypeReference.InferTypeContext(type),
                    scope,
                    nullable);
            }
            else
            {
                return new ClrTypeReference(
                    type,
                    context ?? TypeContext.None,
                    scope,
                    nullable);
            }
        }
    }
}
