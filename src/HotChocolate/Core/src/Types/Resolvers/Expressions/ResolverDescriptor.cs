using System;

namespace HotChocolate.Resolvers.Expressions
{
    /// <summary>
    /// Describes a resolver that is based on a resolver type.
    /// </summary>
    public class ResolverDescriptor
    {
        public ResolverDescriptor(
            Type resolverType,
            Type sourceType,
            FieldMember field)
        {
            ResolverType = resolverType
                ?? throw new ArgumentNullException(nameof(resolverType));
            SourceType = sourceType
                ?? throw new ArgumentNullException(nameof(sourceType));
            Field = field
                ?? throw new ArgumentNullException(nameof(field));
        }

        public ResolverDescriptor(
            Type sourceType,
            FieldMember field)
        {
            SourceType = sourceType
                ?? throw new ArgumentNullException(nameof(sourceType));
            Field = field
                ?? throw new ArgumentNullException(nameof(field));
        }

        public Type ResolverType { get; }

        public Type SourceType { get; }

        public FieldMember Field { get; }
    }
}
