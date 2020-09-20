using HotChocolate.Internal;
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
            string? scope)
        {
            Context = context;
            Scope = scope;
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

            return true;
        }

        public abstract bool Equals(ITypeReference? other);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as ITypeReference);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 0;

                if (Scope is { })
                {
                    hash ^= Scope.GetHashCode() * 397;
                }

                return hash;
            }
        }

        public static SchemaTypeReference Create(
            ITypeSystemMember type,
            string? scope = null)
        {
            if (scope is null && type is IHasScope withScope && withScope.Scope is { })
            {
                scope = withScope.Scope;
            }
            return new SchemaTypeReference(type, scope: scope);
        }

        public static SyntaxTypeReference Create(
            ITypeNode type,
            TypeContext context = TypeContext.None,
            string? scope = null) =>
            new SyntaxTypeReference(type, context, scope);

        public static SyntaxTypeReference Create(
            NameString typeName,
            TypeContext context = TypeContext.None,
            string? scope = null) =>
            new SyntaxTypeReference(new NamedTypeNode(typeName), context, scope);

        public static ExtendedTypeReference Create(
            IExtendedType type,
            TypeContext context = TypeContext.None,
            string? scope = null)
        {
            if (type.IsSchemaType)
            {
                return new ExtendedTypeReference(
                    type,
                    InferTypeContext(type),
                    scope);
            }
            else
            {
                return new ExtendedTypeReference(
                    type,
                    context,
                    scope);
            }
        }
    }
}
