namespace GreenDonut;

/// <summary>
/// Specifies the service scope for a DataLoader.
/// </summary>
public enum DataLoaderServiceScope
{
    /// <summary>
    /// Takes the default scope setting.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Defines that the DataLoader should have a specific service scope.
    /// </summary>
    DataLoaderScope = 1,

    /// <summary>
    /// Defines that the DataLoader should resolve services
    /// from the passed in <see cref="IServiceProvider"/>.
    /// </summary>
    OriginalScope = 2,
}
