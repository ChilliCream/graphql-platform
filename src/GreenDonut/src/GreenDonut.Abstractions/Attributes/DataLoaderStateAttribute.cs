namespace GreenDonut;

/// <summary>
/// Marks parameters that shall be mapped from the DataLoader state when using source generated DataLoader.
/// </summary>
/// <param name="key">
/// The key that shall be used to map the parameter from the DataLoader state.
/// </param>
[AttributeUsage(AttributeTargets.Parameter)]
public class DataLoaderStateAttribute(string key) : Attribute
{
    /// <summary>
    /// Gets the key that shall be used to map the parameter from the DataLoader state.
    /// </summary>
    public string Key { get; } = key;
}
