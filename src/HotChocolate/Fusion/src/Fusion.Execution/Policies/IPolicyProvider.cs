using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Provides authorization policies as an observable stream of complete snapshots.
/// </summary>
/// <remarks>
/// Each update replaces the complete set previously published by that provider. A subscriber
/// synchronously receives the current snapshot when it subscribes. Policies must be safe for
/// concurrent evaluation.
/// </remarks>
public interface IPolicyProvider
    : IObservable<ImmutableArray<IPolicy>>
    , IAsyncDisposable;
