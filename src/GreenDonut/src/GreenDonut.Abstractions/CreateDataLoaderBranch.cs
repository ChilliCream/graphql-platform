namespace GreenDonut;

/// <summary>
/// Creates a branched DataLoader with a specific branch key.
/// </summary>
/// <typeparam name="TKey">
/// The type of the DataLoader key.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the DataLoader value.
/// </typeparam>
/// <typeparam name="TState">
/// The custom state that is passed into the factory.
/// </typeparam>
public delegate IDataLoader CreateDataLoaderBranch<out TKey, TValue, in TState>(
    string branchKey,
    IDataLoader<TKey, TValue> dataLoader,
    TState state)
    where TKey : notnull;
