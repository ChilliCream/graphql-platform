using System;

namespace HotChocolate.Types
{
    public static class CostInterfaceFieldDescriptorExtensions
    {
        public static IInterfaceFieldDescriptor Cost(
            this IInterfaceFieldDescriptor descriptor,
            int complexity)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new CostDirective(complexity));
        }

        public static IInterfaceFieldDescriptor Cost(
            this IInterfaceFieldDescriptor descriptor,
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

        public static IInterfaceFieldDescriptor Cost(
            this IInterfaceFieldDescriptor descriptor,
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
