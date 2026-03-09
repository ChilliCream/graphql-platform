using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

internal interface ITypeConfigurationProvider
{
    /// <summary>
    /// Gets the inner type configuration if it is still available.
    /// </summary>
    ITypeConfiguration? Configuration { get; }
}
