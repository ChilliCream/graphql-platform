using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers.CodeGeneration
{
    /// <summary>
    /// Describes a resolver that is based on a resolver type.
    /// </summary>
    internal class ResolverDescriptor
        : IFieldResolverDescriptor
    {
        public ResolverDescriptor(
            Type sourceType,
            FieldMember field)
            : this(field?.Member.ReflectedType, sourceType, field)
        {
        }

        public ResolverDescriptor(
            Type sourceType,
            FieldMember field,
            ArgumentDescriptor argument)
            : this(field?.Member.ReflectedType,
                sourceType, field, new[] { argument })
        {
        }

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

            if (field.Member is MethodInfo m)
            {
                Arguments = FieldResolverDiscoverer
                    .DiscoverArguments(m, sourceType);
                IsAsync = typeof(Task).IsAssignableFrom(m.ReturnType);
                IsMethod = true;
            }
            else
            {
                Arguments = Array.Empty<ArgumentDescriptor>();
            }
        }

        public ResolverDescriptor(
            Type resolverType,
            Type sourceType,
            FieldMember field,
            IReadOnlyCollection<ArgumentDescriptor> arguments)
        {
            ResolverType = resolverType
                ?? throw new ArgumentNullException(nameof(resolverType));
            SourceType = sourceType
                ?? throw new ArgumentNullException(nameof(sourceType));
            Field = field
                ?? throw new ArgumentNullException(nameof(field));
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));

            if (field.Member is MethodInfo m)
            {
                IsAsync = typeof(Task).IsAssignableFrom(m.ReturnType);
                IsMethod = true;
            }
        }

        public Type ResolverType { get; }

        public Type SourceType { get; }

        public FieldMember Field { get; }

        public IReadOnlyCollection<ArgumentDescriptor> Arguments { get; }

        public bool IsAsync { get; }

        public bool IsMethod { get; }

        public bool IsProperty => !IsMethod;
    }
}
