using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Fusion.Policies.Auditing;

namespace HotChocolate.Fusion;

/// <summary>
/// An audit provider that keeps every scope it was asked to begin.
/// </summary>
public sealed class RecordingAuditProvider : IAuditProvider
{
    private readonly ConcurrentQueue<Scope> _scopes = [];

    public ImmutableArray<string> Scopes => [.. _scopes.Select(s => s.Describe())];

    public IAuditTrail CreateTrail(IServiceProvider requestServices) => new Trail(this);

    private sealed class Trail(RecordingAuditProvider provider) : IAuditTrail
    {
        public string InvocationId => "invocation";

        public IAuditScope BeginRequest(AuditScopeInfo info, ClaimsPrincipal user)
        {
            var scope = new Scope(this, info, user);
            provider._scopes.Enqueue(scope);

            return scope;
        }
    }

    private sealed class Scope(IAuditTrail trail, AuditScopeInfo info, ClaimsPrincipal user)
        : AuditScope(trail, info, user)
    {
        private ImmutableArray<AuditLogEntry> _entries = [];
        private int _commits;

        public string Describe()
        {
            var entries = _entries.Select(
                e => $"{e.Coordinate} @{e.DirectiveName}({e.PolicyName}) {e.Outcome} {e.Reason ?? "-"}");

            return $"commits={_commits} | {string.Join("; ", entries)} | failure={FailureReason ?? "-"}";
        }

        protected override ValueTask OnCommitAsync(
            ImmutableArray<AuditLogEntry> entries,
            CancellationToken cancellationToken)
        {
            _entries = entries;
            _commits++;

            return ValueTask.CompletedTask;
        }
    }
}
