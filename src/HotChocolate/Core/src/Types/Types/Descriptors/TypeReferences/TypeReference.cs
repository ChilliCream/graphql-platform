using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Types.Descriptors.SchemaTypeReference;

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
            Scope = scope;
            Nullable = nullable;
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

            for (var i = 0; i < Nullable.Length; i++)
            {
                if (Nullable[i] != other.Nullable[i])
                {
                    return false;
                }
            }

            return true;
        }

        public abstract bool Equals(ITypeReference? other);

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

        public static SchemaTypeReference Create(
            ITypeSystemMember type,
            string? scope = null,
            bool[]? nullable = null) =>
            new SchemaTypeReference(type, scope: scope, nullable: nullable);

        public static SyntaxTypeReference Create(
            ITypeNode type,
            TypeContext context = TypeContext.None,
            string? scope = null,
            bool[]? nullable = null) =>
            new SyntaxTypeReference(type, context, scope, nullable);

        public static SyntaxTypeReference Create(
            string typeName,
            TypeContext context = TypeContext.None,
            string? scope = null,
            bool[]? nullable = null) =>
            new SyntaxTypeReference(new NamedTypeNode(typeName), context, scope, nullable);

        public static ClrTypeReference Create<T>(
            TypeContext context = TypeContext.None,
            string? scope = null,
            bool[]? nullable = null) =>
            Create(typeof(T), context, scope, nullable);

        public static ClrTypeReference Create(
            Type type,
            TypeContext context = TypeContext.None,
            string? scope = null,
            bool[]? nullable = null)
        {
            if (typeof(ITypeSystemMember).IsAssignableFrom(type))
            {
                return new ClrTypeReference(
                    type,
                    InferTypeContext(type),
                    scope,
                    nullable);
            }
            else
            {
                return new ClrTypeReference(
                    type,
                    context,
                    scope,
                    nullable);
            }
        }
    }
}
