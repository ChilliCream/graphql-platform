using GreenDonut.DependencyInjection;

namespace HotChocolate.Fetching;

/// <summary>
/// This helper will start a new DataLoader context or ensure that one exists.
/// </summary>
internal interface IDataLoaderContextHelper
{
    /// <summary>
    /// Creates a new <see cref="IDataLoaderContext"/> and stores it in the execution context.
    /// </summary>
    /// <returns></returns>
    IDataLoaderContext CreateNewContext();

    /// <summary>
    /// Ensures that a <see cref="IDataLoaderContext"/> is initialized.
    /// </summary>
    /// <returns></returns>
    IDataLoaderContext EnsureContextExists();
}