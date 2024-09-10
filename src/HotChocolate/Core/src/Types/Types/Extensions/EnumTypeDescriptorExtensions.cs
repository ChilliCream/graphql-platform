namespace HotChocolate.Types;

/// <summary>
/// Provides extensions to the <see cref="IEnumTypeDescriptor"/> and
/// <see cref="IEnumTypeDescriptor{T}"/>.
/// </summary>
public static class EnumTypeDescriptorExtensions
{
    /// <summary>
    /// Ignores the given enum value for the schema creation.
    /// This enum will not be included into the GraphQL schema.
    /// </summary>
    /// <param name="descriptor">
    /// The enum type descriptor.
    /// </param>
    /// <param name="value">
    /// The enum value that shall be ignored.
    /// </param>
    /// <typeparam name="T">
    /// The enum value type.
    /// </typeparam>
    /// <returns>
    /// Returns the enum type descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c> or
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor<T> Ignore<T>(
        this IEnumTypeDescriptor<T> descriptor,
        T value)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        descriptor.Value(value).Ignore();
        return descriptor;
    }

    /// <summary>
    /// Ignores the given enum value for the schema creation.
    /// This enum will not be included into the GraphQL schema.
    /// </summary>
    /// <param name="descriptor">
    /// The enum type descriptor.
    /// </param>
    /// <param name="value">
    /// The enum value that shall be ignored.
    /// </param>
    /// <typeparam name="T">
    /// The enum value type.
    /// </typeparam>
    /// <returns>
    /// Returns the enum type descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c> or
    /// <paramref name="value"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor Ignore<T>(
        this IEnumTypeDescriptor descriptor,
        T value)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        descriptor.Value(value).Ignore();
        return descriptor;
    }
}
