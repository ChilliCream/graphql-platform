namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Provides the data document that backs a policy language as an observable stream of materialized
/// snapshots.
/// </summary>
/// <remarks>
/// A subscriber receives a freshly materialized snapshot synchronously when data is available, and
/// each later emission delivers a new snapshot. Ownership of every delivered snapshot transfers to
/// the subscriber, which disposes it when it is no longer needed. A replayed initial value is always
/// materialized anew for the subscribing observer, so a provider never re-delivers an instance it has
/// already handed out and never disposes what it has delivered.
/// </remarks>
public interface IPolicyDataProvider
    : IObservable<PolicyDataSnapshot>
    , IAsyncDisposable;
