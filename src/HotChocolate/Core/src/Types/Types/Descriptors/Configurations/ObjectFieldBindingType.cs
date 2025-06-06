namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Describes what a field filter binds to.
/// </summary>
public enum ObjectFieldBindingType
{
    /// <summary>
    /// Binds to a property
    /// </summary>
    Property,

    /// <summary>
    /// Binds to a GraphQL field
    /// </summary>
    Field
}
