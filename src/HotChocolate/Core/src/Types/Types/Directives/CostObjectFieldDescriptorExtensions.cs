using System;

namespace HotChocolate.Types;

public static class CostObjectFieldDescriptorExtensions
{
    /// <summary>
    /// The cost directive can be used to express the expected
    /// cost that a resolver incurs on the system.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="complexity">
    /// The complexity of the field.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Cost(
        this IObjectFieldDescriptor descriptor,
        int complexity)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new CostDirective(complexity));
    }

    /// <summary>
    /// The cost directive can be used to express the expected
    /// cost that a resolver incurs on the system.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="complexity">
    /// The complexity of the field.
    /// </param>
    /// <param name="multiplier">
    /// The multiplier path.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Cost(
        this IObjectFieldDescriptor descriptor,
        int complexity,
        MultiplierPathString multiplier)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new CostDirective(complexity, multiplier));
    }

    /// <summary>
    /// The cost directive can be used to express the expected
    /// cost that a resolver incurs on the system.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="complexity">
    /// The complexity of the field.
    /// </param>
    /// <param name="multipliers">
    /// The multiplier paths.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Cost(
        this IObjectFieldDescriptor descriptor,
        int complexity,
        params MultiplierPathString[] multipliers)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(new CostDirective(complexity, multipliers));
    }
}
