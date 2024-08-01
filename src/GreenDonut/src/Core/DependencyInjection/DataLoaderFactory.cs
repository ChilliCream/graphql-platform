namespace GreenDonut.DependencyInjection;

/// <summary>
/// Represents a factory that creates a DataLoader instance.
/// </summary>
public delegate IDataLoader DataLoaderFactory(IServiceProvider serviceProvider);

/// <summary>
/// Represents a factory that creates a DataLoader instance.
/// </summary>
public delegate T DataLoaderFactory<out T>(IServiceProvider serviceProvider)
    where T : IDataLoader;
