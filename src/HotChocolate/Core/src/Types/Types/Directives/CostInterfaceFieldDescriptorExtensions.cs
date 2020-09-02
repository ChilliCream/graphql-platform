using System;

namespace HotChocolate.Types
{
    public static class CostInterfaceFieldDescriptorExtensions
    {
        public static IInterfaceFieldDescriptor Cost(
            this IInterfaceFieldDescriptor descriptor,
            int complexity)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new CostDirective(complexity));
        }

        public static IInterfaceFieldDescriptor Cost(
            this IInterfaceFieldDescriptor descriptor,
            int complexity,
            MultiplierPathString multiplier)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                new CostDirective(complexity, multiplier));
        }

        public static IInterfaceFieldDescriptor Cost(
            this IInterfaceFieldDescriptor descriptor,
            int complexity,
            params MultiplierPathString[] multipliers)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                new CostDirective(complexity, multipliers));
        }
    }
}
