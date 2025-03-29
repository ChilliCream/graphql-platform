namespace GreenDonut;

/// <summary>
/// The key of a cached task.
/// </summary>
public readonly record struct PromiseCacheKey(string Type, object Key);
