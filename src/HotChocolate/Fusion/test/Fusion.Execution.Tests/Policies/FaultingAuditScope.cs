using System.Collections.Immutable;
using HotChocolate.Fusion.Policies.Audit;

namespace HotChocolate.Fusion.Policies;

internal sealed class FaultingAuditScope(
    IAuditScope inner,
    (int Call, Exception Failure)? recordFailure,
    Exception? failFailure) : IAuditScope
{
    private int _recordCalls;

    public IAuditTrail Trail => inner.Trail;

    public AuditScopeInfo Info => inner.Info;

    public AuditSubject Subject => inner.Subject;

    public ImmutableDictionary<string, string> Context => inner.Context;

    public bool IsRecording => inner.IsRecording;

    public void Record(in AuditLogEntry entry)
    {
        if (++_recordCalls == recordFailure?.Call)
        {
            throw recordFailure.Value.Failure;
        }

        inner.Record(entry);
    }

    public void Fail(Exception exception)
    {
        if (failFailure is not null)
        {
            throw failFailure;
        }

        inner.Fail(exception);
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken) => inner.CommitAsync(cancellationToken);
}
