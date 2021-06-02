using System;

namespace HotChocolate.Types
{
    /// <summary>
    /// Provides configuration methods to <see cref="IObjectFieldDescriptor"/>.
    /// </summary>
    public static class ObjectFieldDescriptorExtensions
    {
        /// <summary>
        /// Marks a field as serial executable which will ensure that the execution engine 
        /// synchronizes resolver execution around the marked field and ensures that 
        /// no other field is executed in parallel.
        /// </summary>
        public static IObjectFieldDescriptor Serial(this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend().OnBeforeCreate(c => c.IsParallelExecutable = false);
            return descriptor;
        }

        /// <summary>
        /// Marks a field as parallel executable which will allow the execution engine 
        /// to execute this field in parallel with other resolvers.
        /// </summary>
        public static IObjectFieldDescriptor Parallel(this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend().OnBeforeCreate(c => c.IsParallelExecutable = true);
            return descriptor;
        }
    }
}