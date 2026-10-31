using System.Collections.Immutable;

namespace HotChocolate.Fusion.Policies.Audit;

internal sealed class NoOpAuditScope : IAuditScope
{
    public static NoOpAuditScope Instance { get; } = new();

    private NoOpAuditScope()
    {
    }

    public IAuditTrail Trail => NoOpAuditTrail.Instance;

    public AuditScopeInfo Info => default;

    public AuditSubject Subject => default;

    public ImmutableDictionary<string, string> Context { get; } =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<string, string>.Empty;
#endif

    public bool IsRecording => false;

    public void Record(in AuditLogEntry entry)
    {
    }

    public void Fail(Exception exception)
    {
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
