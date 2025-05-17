using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

internal interface IHasTypeConfiguration
{
    /// <summary>
    /// Gets the inner type configuration if it is still available.
    /// </summary>
    ITypeConfiguration? Configuration { get; }
}
