namespace HotChocolate.Types;

/// <summary>
/// Provides extensions to the <see cref="IObjectFieldDescriptor"/> for the mutation convention.
/// </summary>
public static class MutationObjectFieldDescriptorExtensions
{
    /// <summary>
    /// The UseMutationConvention allows to override the global mutation convention settings
    /// on a per field basis.
    /// </summary>
    /// <param name="descriptor">The descriptor of the field</param>
    /// <param name="options">
    /// The options that shall override the global mutation convention options.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor UseMutationConvention(
        this IObjectFieldDescriptor descriptor,
        MutationFieldOptions options = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Extend().OnBeforeNaming((c, d) =>
        {
            c.ContextData
                .GetMutationFields()
                .Add(new(d,
                    options.InputTypeName,
                    options.InputArgumentName,
                    options.PayloadTypeName,
                    options.PayloadFieldName,
                    options.PayloadErrorTypeName,
                    options.PayloadErrorsFieldName,
                    !options.Disable));
        });

        return descriptor;
    }
}
