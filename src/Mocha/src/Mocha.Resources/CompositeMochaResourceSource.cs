using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Mocha.Resources;

/// <summary>
/// A <see cref="MochaResourceSource"/> whose contents are the flat aggregation of one
/// or more child sources, with a single composite change token that fires whenever any
/// child fires.
/// </summary>
/// <remarks>
/// <para>
/// Modelled directly on ASP.NET Core's <c>CompositeEndpointDataSource</c>: lazy
/// initialization of both the snapshot cache and the change token, swap-before-cancel
/// ordering so callback re-entrancy can neither deadlock nor stack-overflow, and
/// per-child <c>ChangeToken.OnChange</c> registrations that auto-re-register on each
/// child's next token.
/// </para>
/// <para>
/// Consumer rule: read <see cref="GetChangeToken"/> <em>before</em> reading
/// <see cref="Resources"/>; otherwise a change that fires between the two reads is
/// missed (you'd get a stale snapshot paired with a fresh token).
/// </para>
/// </remarks>
public sealed class CompositeMochaResourceSource : MochaResourceSource, IDisposable
{
    private readonly object _lock = new();
    private readonly ICollection<MochaResourceSource> _sources;

    private List<MochaResource>? _resources;
    private IChangeToken? _consumerChangeToken;
    private CancellationTokenSource? _cts;
    private List<IDisposable>? _changeTokenRegistrations;
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="CompositeMochaResourceSource"/> over the supplied
    /// child sources.
    /// </summary>
    /// <param name="sources">The child sources whose resources are aggregated by this composite.</param>
    public CompositeMochaResourceSource(IEnumerable<MochaResourceSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var list = new List<MochaResourceSource>();
        foreach (var source in sources)
        {
            list.Add(source);
        }

        _sources = list;
    }

    /// <summary>
    /// Gets the child sources composed by this instance.
    /// </summary>
    public IEnumerable<MochaResourceSource> Sources => _sources;

    /// <inheritdoc />
    public override IReadOnlyList<MochaResource> Resources
    {
        get
        {
            EnsureResourcesInitialized();
            return _resources;
        }
    }

    /// <inheritdoc />
    public override IChangeToken GetChangeToken()
    {
        EnsureChangeTokenInitialized();
        return _consumerChangeToken;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        List<IDisposable>? disposables = null;

        lock (_lock)
        {
            _disposed = true;

            foreach (var source in _sources)
            {
                if (source is IDisposable disposableSource)
                {
                    disposables ??= [];
                    disposables.Add(disposableSource);
                }
            }

            if (_changeTokenRegistrations is { Count: > 0 })
            {
                disposables ??= [];
                disposables.AddRange(_changeTokenRegistrations);
            }
        }

        // Dispose outside the lock — registration disposal can block on user code that may try to take the lock.
        if (disposables is not null)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }

    [MemberNotNull(nameof(_resources))]
    private void EnsureResourcesInitialized()
    {
        if (_resources is not null)
        {
            return;
        }

        lock (_lock)
        {
            if (_resources is not null)
            {
                return;
            }

            // Token must be set up before the snapshot is captured, otherwise a child change that fires
            // during snapshot construction would be lost — the new snapshot would already include it but
            // the token wouldn't have rebuilt.
            EnsureChangeTokenInitialized();

            CreateResourcesUnsynchronized();
        }
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void EnsureChangeTokenInitialized()
    {
        if (_consumerChangeToken is not null)
        {
            return;
        }

        lock (_lock)
        {
            if (_consumerChangeToken is not null)
            {
                return;
            }

            // First-time initialization: treat as a collection change so we wire up child registrations.
            CreateChangeTokenUnsynchronized(collectionChanged: true);
        }
    }

    private void HandleChange(bool collectionChanged)
    {
        CancellationTokenSource? oldTokenSource;
        List<IDisposable>? oldChangeTokenRegistrations;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            // Capture current state, swap to a fresh token *before* cancelling the old one — so any
            // re-registration that happens inside a consumer callback lands on the fresh token, not the
            // one currently firing. Prevents both deadlock and stack overflow.
            oldTokenSource = _cts;
            oldChangeTokenRegistrations = _changeTokenRegistrations;

            // Don't create a new change token if no one is listening.
            if (oldTokenSource is not null)
            {
                CreateChangeTokenUnsynchronized(collectionChanged);
            }

            // Don't refresh the snapshot if no one has read it yet.
            if (_resources is not null)
            {
                CreateResourcesUnsynchronized();
            }
        }

        // Outside the lock: dispose old per-child registrations only when the child set itself changed
        // (otherwise the existing registrations auto-re-register on each child's next token).
        if (collectionChanged && oldChangeTokenRegistrations is not null)
        {
            foreach (var registration in oldChangeTokenRegistrations)
            {
                registration.Dispose();
            }
        }

        // Fire consumer callbacks last, outside the lock, after the new token is in place.
        oldTokenSource?.Cancel();
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void CreateChangeTokenUnsynchronized(bool collectionChanged)
    {
        var cts = new CancellationTokenSource();

        if (collectionChanged)
        {
            _changeTokenRegistrations = [];
            foreach (var source in _sources)
            {
                _changeTokenRegistrations.Add(
                    ChangeToken.OnChange(
                        source.GetChangeToken,
                        () => HandleChange(collectionChanged: false)));
            }
        }

        _cts = cts;
        _consumerChangeToken = new CancellationChangeToken(cts.Token);
    }

    [MemberNotNull(nameof(_resources))]
    private void CreateResourcesUnsynchronized()
    {
        var resources = new List<MochaResource>();

        foreach (var source in _sources)
        {
            resources.AddRange(source.Resources);
        }

        // Only cache after a successful build so a partial failure doesn't poison the snapshot.
        _resources = resources;
    }
}
