using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Fusion.Policies.Audit;

namespace HotChocolate.Fusion.Policies;

internal sealed class RecordingAuditScope(
    IAuditTrail trail,
    AuditScopeInfo info,
    ClaimsPrincipal user,
    ImmutableDictionary<string, string> context,
    Func<int> countSourceSchemaRequests,
    Exception? commitFailure)
    : AuditScope(trail, info, user, context)
{
    public ImmutableArray<AuditLogEntry> Entries { get; private set; } = [];

    public int CommitCount { get; private set; }

    public int SourceSchemaRequestsAtCommit { get; private set; }

    protected override ValueTask OnCommitAsync(
        ImmutableArray<AuditLogEntry> entries,
        CancellationToken cancellationToken)
    {
        Entries = entries;
        CommitCount++;
        SourceSchemaRequestsAtCommit = countSourceSchemaRequests();

        return commitFailure is null
            ? ValueTask.CompletedTask
            : ValueTask.FromException(commitFailure);
    }
}
