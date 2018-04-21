using System.Linq;

namespace HotChocolate.Resolvers
{
    public static class FieldResolverDescriptorExtensions
    {
        public static int ArgumentCount(this FieldResolverDescriptor descriptor)
        {
            return descriptor.ArgumentDescriptors
                .Where(t => t.Kind == FieldResolverArgumentKind.Argument)
                .Count();
        }
    }
}
