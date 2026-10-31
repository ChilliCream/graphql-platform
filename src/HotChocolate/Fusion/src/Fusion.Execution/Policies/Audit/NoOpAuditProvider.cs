namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// The default provider, which keeps no entries.
/// </summary>
internal sealed class NoOpAuditProvider : IAuditProvider
{
    public static NoOpAuditProvider Instance { get; } = new();

    private NoOpAuditProvider()
    {
    }

    public IAuditTrail CreateTrail(IServiceProvider requestServices) => NoOpAuditTrail.Instance;
}
