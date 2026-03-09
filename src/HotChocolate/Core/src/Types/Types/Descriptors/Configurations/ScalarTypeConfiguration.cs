namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Defines the properties of a GraphQL scalar type.
/// </summary>
public sealed class ScalarTypeConfiguration : TypeConfiguration
{
    public Uri? SpecifiedBy { get; set; }
}
