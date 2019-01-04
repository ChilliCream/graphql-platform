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
            NameString multiplier)
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
            params NameString[] multipliers)
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
