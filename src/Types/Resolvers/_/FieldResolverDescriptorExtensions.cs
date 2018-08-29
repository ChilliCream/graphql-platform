using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Resolvers
{
    internal static class FieldResolverDescriptorExtensions
    {
        public static bool AnyArgument(this IFieldResolverDescriptor descriptor)
        {
            return descriptor.Arguments
                .Any(t => t.Kind == ArgumentKind.Argument);
        }

        public static IEnumerable<ArgumentDescriptor> Arguments(
            this IFieldResolverDescriptor descriptor)
        {
            return descriptor.Arguments
                .Where(t => t.Kind == ArgumentKind.Argument);
        }
    }
}
