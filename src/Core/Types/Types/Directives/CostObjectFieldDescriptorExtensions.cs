using System;

namespace HotChocolate.Types
{
    public static class CostObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Cost(
            this IObjectFieldDescriptor descriptor,
            int complexity)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new CostDirective(complexity));
        }

        public static IObjectFieldDescriptor Cost(
            this IObjectFieldDescriptor descriptor,
            int complexity,
            string multiplier)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                new CostDirective(complexity, multiplier));
        }

        public static IObjectFieldDescriptor Cost(
            this IObjectFieldDescriptor descriptor,
            int complexity,
            params string[] multipliers)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                new CostDirective(complexity, multipliers));
        }
    }
}
