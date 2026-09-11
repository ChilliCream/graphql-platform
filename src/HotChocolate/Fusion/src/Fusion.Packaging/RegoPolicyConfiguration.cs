namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Represents a Rego policy and its GraphQL data requirements in a Fusion Archive.
/// </summary>
public sealed class RegoPolicyConfiguration
{
    private readonly Func<CancellationToken, Task<Stream>> _openReadPolicy;
    private readonly Func<CancellationToken, Task<Stream>> _openReadRequirements;

    internal RegoPolicyConfiguration(
        string name,
        Version version,
        Func<CancellationToken, Task<Stream>> openReadPolicy,
        Func<CancellationToken, Task<Stream>> openReadRequirements)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(openReadPolicy);
        ArgumentNullException.ThrowIfNull(openReadRequirements);

        Name = name;
        FormatVersion = version;
        _openReadPolicy = openReadPolicy;
        _openReadRequirements = openReadRequirements;
    }

    /// <summary>
    /// Gets the policy name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the Rego policy format version.
    /// </summary>
    public Version FormatVersion { get; }

    /// <summary>
    /// Opens the Rego policy implementation for reading.
    /// </summary>
    public Task<Stream> OpenReadPolicyAsync(CancellationToken cancellationToken = default)
        => _openReadPolicy(cancellationToken);

    /// <summary>
    /// Opens the GraphQL data requirements for reading.
    /// </summary>
    public Task<Stream> OpenReadRequirementsAsync(CancellationToken cancellationToken = default)
        => _openReadRequirements(cancellationToken);
}
