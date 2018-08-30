using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers.CodeGeneration
{
    /// <summary>
    /// Describes a resolver that is based on the data types.
    /// </summary>
    internal class SourceResolverDescriptor
        : IFieldResolverDescriptor
    {
        public SourceResolverDescriptor(FieldMember field)
            : this(field?.Member.ReflectedType, field)
        {
        }

        public SourceResolverDescriptor(
                FieldMember field,
                ArgumentDescriptor argument)
            : this(field?.Member.ReflectedType, field, new[] { argument })
        {
        }

        public SourceResolverDescriptor(
            Type sourceType,
            FieldMember field)
        {
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

        public SourceResolverDescriptor(
            Type sourceType,
            FieldMember field,
            IReadOnlyCollection<ArgumentDescriptor> arguments)
        {
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

        public Type SourceType { get; }

        public FieldMember Field { get; }

        public IReadOnlyCollection<ArgumentDescriptor> Arguments { get; }

        public bool IsAsync { get; }

        public bool IsMethod { get; }

        public bool IsProperty => IsMethod;
    }
}
