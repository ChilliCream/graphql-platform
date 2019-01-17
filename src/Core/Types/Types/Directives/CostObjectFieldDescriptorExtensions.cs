﻿using System;

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
            MultiplierPathString multiplier)
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
            params MultiplierPathString[] multipliers)
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
