namespace HotChocolate.Data;

/// <summary>
/// The paging key can be used when offloading paging logic to a DataLoader.
/// </summary>
/// <param name="Key">
/// The actual parent key that is used to load the page.
/// </param>
/// <param name="PagingArgs">
/// The paging arguments that shall be used to load the page.
/// </param>
/// <typeparam name="T">
/// The type of the parent key.
/// </typeparam>
public readonly record struct PageKey<T>(T Key, PagingArguments PagingArgs);