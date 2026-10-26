using System.Collections.Immutable;

namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// The policy decisions of one variable set, which the gateway commits exactly once before it
/// executes the variable set. A scope is owned by the evaluation of its variable set and is not
/// thread-safe.
/// </summary>
public interface IAuditScope
{
    /// <summary>
    /// Gets the trail that opened the scope.
    /// </summary>
    IAuditTrail Trail { get; }

    /// <summary>
    /// Gets the identity of the variable set.
    /// </summary>
    AuditScopeInfo Info { get; }

    /// <summary>
    /// Gets the principal of the request as values captured once for the scope.
    /// </summary>
    AuditSubject Subject { get; }

    /// <summary>
    /// Gets the provider data attached when the scope was opened, which is empty when the
    /// provider attaches none.
    /// </summary>
    ImmutableDictionary<string, string> Context { get; }

    /// <summary>
    /// Gets a value that indicates whether the scope keeps entries. The gateway only builds
    /// entries for a recording scope.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Adds an entry to the scope.
    /// </summary>
    /// <param name="entry">
    /// The entry to add.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The scope is already committed.
    /// </exception>
    void Record(in AuditLogEntry entry);

    /// <summary>
    /// Records the failure that ended the evaluation of the variable set. The first failure wins.
    /// </summary>
    /// <param name="exception">
    /// The exception that ended the evaluation.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The scope is already committed.
    /// </exception>
    void Fail(Exception exception);

    /// <summary>
    /// Makes the entries durable and freezes the scope.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that signals that the request was aborted.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The scope is already committed.
    /// </exception>
    ValueTask CommitAsync(CancellationToken cancellationToken);
}
