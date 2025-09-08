namespace GreenDonut.DependencyInjection;

/// <summary>
/// The DataLoader scope provides access to the DataLoader bound to the current execution.
/// </summary>
public interface IDataLoaderScope
{
    /// <summary>
    /// Gets a <see cref="IDataLoader"/> from the current execution scope; or, creates a new instance for this scope.
    /// </summary>
    /// <param name="createDataLoader">
    /// The factory to create the new <see cref="IDataLoader"/> instance.
    /// </param>
    /// <param name="name">
    /// The name of the <see cref="IDataLoader"/>. If no name is specified the name will be inferred from the type.
    /// </param>
    /// <typeparam name="T">
    /// The DataLoader type.
    /// </typeparam>
    /// <returns>
    /// Returns a <see cref="IDataLoader"/> instance from the current execution scope.
    /// </returns>
    T GetDataLoader<T>(DataLoaderFactory<T> createDataLoader, string? name = null) where T : IDataLoader;

    /// <summary>
    /// Gets a <see cref="IDataLoader"/> from the current execution scope; or, creates a new instance for this scope.
    /// </summary>
    /// <typeparam name="T">
    /// The DataLoader type.
    /// </typeparam>
    /// <returns>
    /// Returns a <see cref="IDataLoader"/> instance from the current execution scope.
    /// </returns>
    T GetDataLoader<T>() where T : IDataLoader;
}
