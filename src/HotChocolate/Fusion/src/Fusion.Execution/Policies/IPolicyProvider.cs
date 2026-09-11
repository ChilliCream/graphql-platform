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
/// <remarks>
/// A provider registered on the gateway's schema services (for example through
/// <c>ConfigureSchemaServices</c>) is wrapped in a schema-scoped composite and held for the
/// lifetime of the schema, regardless of the lifetime the registration itself declared: a
/// scoped or transient registration is effectively promoted to singleton for as long as that
/// schema's services are alive. Register a provider that is safe to share for that lifetime.
/// </remarks>
public interface IPolicyProvider
    : IObservable<ImmutableArray<IPolicy>>
    , IAsyncDisposable;
