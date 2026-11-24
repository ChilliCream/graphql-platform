namespace HotChocolate.Types;

/// <summary>
/// Provides execution group configuration methods for <see cref="IObjectFieldDescriptor"/>.
/// </summary>
public static class ExecutionGroupObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Makes the field the root of an execution group. The selections named by the entries
    /// run one after the other in ascending priority for each result of the field.
    /// </summary>
    /// <param name="descriptor">The object field descriptor.</param>
    /// <param name="entries">The members of the group.</param>
    /// <returns>The object field descriptor for configuration chaining.</returns>
    public static IObjectFieldDescriptor ExecutionGroup(
        this IObjectFieldDescriptor descriptor,
        params ExecutionGroupEntry[] entries)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(entries);

        var configuration = new ExecutionGroupConfiguration(entries);
        descriptor.Extend().OnBeforeCreate(c => c.ExecutionGroup = configuration);
        return descriptor;
    }
}
