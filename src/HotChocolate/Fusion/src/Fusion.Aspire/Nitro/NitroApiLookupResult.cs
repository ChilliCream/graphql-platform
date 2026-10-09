namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Describes the outcome of resolving a Nitro api by its id.
/// </summary>
internal enum NitroApiLookupStatus
{
    /// <summary>
    /// The api exists and its name was resolved.
    /// </summary>
    Found,

    /// <summary>
    /// Nitro knows no api with the requested id.
    /// </summary>
    NotFound,

    /// <summary>
    /// The lookup itself failed, so whether the api exists is unknown.
    /// </summary>
    Failed
}

/// <summary>
/// The outcome of resolving a Nitro api by its id.
/// </summary>
/// <param name="Status">
/// Whether the api exists.
/// </param>
/// <param name="Name">
/// The name of the api, set when <paramref name="Status"/> is
/// <see cref="NitroApiLookupStatus.Found"/>.
/// </param>
internal sealed record NitroApiLookupResult(NitroApiLookupStatus Status, string? Name)
{
    public static NitroApiLookupResult NotFound { get; } = new(NitroApiLookupStatus.NotFound, Name: null);

    public static NitroApiLookupResult Failed { get; } = new(NitroApiLookupStatus.Failed, Name: null);

    public static NitroApiLookupResult Found(string name) => new(NitroApiLookupStatus.Found, name);
}
