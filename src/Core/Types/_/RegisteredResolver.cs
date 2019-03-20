using System;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    internal sealed class RegisteredResolver
    {
        public RegisteredResolver(
            Type sourceType,
            IFieldReference field)
            : this(sourceType, sourceType, field)
        {
        }

        public RegisteredResolver(
            Type resolverType,
            Type sourceType,
            IFieldReference field)
        {
            ResolverType = resolverType
                ?? throw new ArgumentNullException(nameof(resolverType));
            SourceType = sourceType
                ?? throw new ArgumentNullException(nameof(sourceType));
            Field = field
                ?? throw new ArgumentNullException(nameof(field));
        }

        public Type ResolverType { get; }
        public Type SourceType { get; }
        public IFieldReference Field { get; }
        public bool IsSourceResolver => ResolverType == SourceType;
    }
}
