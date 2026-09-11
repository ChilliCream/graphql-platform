namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// The exception raised when a registered <see cref="IRegoDataProvider"/> fails to contribute
/// usable data: either the provider itself failed to produce a snapshot (it threw or timed out),
/// or the snapshot it produced could not be merged with the current FAR data or another provider's
/// data. Reported through <c>IFusionExecutionDiagnosticEvents.PolicyUpdateError</c>.
/// </summary>
public sealed class RegoDataProviderException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="RegoDataProviderException"/>.
    /// </summary>
    /// <param name="providerName">The name the failing provider was registered under.</param>
    /// <param name="innerException">The exception the provider raised, or the merge failure.</param>
    public RegoDataProviderException(string providerName, Exception innerException)
        : base(
            $"The Rego data provider '{providerName}' failed to produce usable data.",
            innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerName);
        ArgumentNullException.ThrowIfNull(innerException);

        ProviderName = providerName;
    }

    /// <summary>
    /// Gets the name the failing provider was registered under.
    /// </summary>
    public string ProviderName { get; }
}
