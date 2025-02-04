namespace GreenDonut;

/// <summary>
/// Allows to group multiple DataLoaders together into a context class
/// that can be used to inject multiple DataLoader at once into classes.
/// </summary>
/// <param name="groupNames">
/// The group names that are used to group multiple DataLoaders together.
/// </param>
[AttributeUsage(
    AttributeTargets.Method
    | AttributeTargets.Class,
    AllowMultiple = true)]
public class DataLoaderGroupAttribute(params string[] groupNames) : Attribute
{
    /// <summary>
    /// Gets the group names that are used to group multiple DataLoaders together.
    /// </summary>
    public string[] GroupNames { get; } = groupNames;
}
