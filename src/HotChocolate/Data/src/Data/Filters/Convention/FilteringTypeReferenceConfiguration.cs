using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Configures a <see cref="FilterInputType{T}"/>
/// </summary>
public abstract class FilteringTypeReferenceConfiguration
{
    private readonly ConfigureFilterInputType _configure;

    protected FilteringTypeReferenceConfiguration(ConfigureFilterInputType configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Specifies if the configuration can handle the type reference.
    /// </summary>
    /// <param name="typeReference">
    /// The type reference.
    /// </param>
    /// <returns>
    /// <c>true</c> if the configuration can handle the type reference otherwise, <c>false</c>.
    /// </returns>
    public abstract bool CanHandle(TypeReference typeReference);

    public void Configure(IFilterInputTypeDescriptor descriptor)
    {
        _configure(descriptor);
    }

    /// <summary>
    /// Creates a new <see cref="FilteringTypeReferenceConfiguration"/> that configures based
    /// on the schema type
    /// </summary>
    public static FilteringTypeReferenceConfiguration ConfigureSchemaType<T>(
        ConfigureFilterInputType descriptor) =>
        new FilteringSchemaTypeReferenceConfiguration<T>(descriptor);
}
