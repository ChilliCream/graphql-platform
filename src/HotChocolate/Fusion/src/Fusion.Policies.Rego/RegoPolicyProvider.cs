using System.Collections.Immutable;
using System.Text;
using ChilliCream.Regorus;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Packaging;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// Compiles the Rego policies and data carried by the Fusion configuration stream and publishes the
/// complete compiled policy set whenever it changes.
/// </summary>
public sealed class RegoPolicyProvider
    : IPolicyProvider
    , IObserver<PolicyContentSnapshot?>
{
    private const string RegoLanguage = "rego";

#if NET9_0_OR_GREATER
    private readonly Lock _publishSync = new();
#else
    private readonly object _publishSync = new();
#endif
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly RegoDataAggregator? _dataAggregator;

    // The real Regorus compiler, or a test-supplied stand-in that lets a test observe every
    // compile attempt (how many ran, what became of each resulting set) or force a specific
    // outcome for scenarios the real compiler cannot be made to reproduce on demand (for
    // example: a candidate whose code compiles against one data document but not another).
    // Always the real compiler outside tests.
    private readonly Func<byte[], IReadOnlyList<PolicyModule>, IReadOnlyList<string>, CompiledPolicySet> _compiler;

    // The provider keeps only the handle it last published. An earlier handle is dropped on
    // rebuild, not retired into a list: the RegoPolicy instances built from it that are still
    // pinned by an in-flight request keep it reachable for as long as they need it, and the
    // compiled policy set's SafeHandle releases the native policy engine memory once nothing
    // references it any more.
    private PolicySetHandle? _currentHandle;

    // The last successfully compiled and published code/data pair. _data is the FAR data document
    // as received (used to detect a real FAR change); _lastMergedData is the exact bytes the last
    // successful CompiledPolicySet.Compile call ran with (the FAR data merged with provider data,
    // or the same as _data when there is no data aggregator). Both are only ever assigned once a
    // compile attempt with this exact pair has succeeded, so a candidate that failed to compile is
    // never mistaken for the current state.
    private Dictionary<string, PolicyContent> _contents = new(StringComparer.Ordinal);

    // Shared library modules (bundle-wide libraries and package-scoped helper modules), compiled
    // alongside _contents into every policy set but never scanned for decisions and never subject to
    // the no-entrypoint '.allow' fallback below.
    private Dictionary<string, PolicyLibraryModule> _libraries = new(StringComparer.Ordinal);
    private byte[]? _data;
    private byte[]? _lastMergedData;

    // The FAR candidate a merge collision is currently keeping pending. Set whenever a new FAR
    // candidate's code compiles (against the last-good data) but its own data collides with the
    // current provider data: a provider-driven retry (OnProviderDataChanged) or the next FAR
    // publish (which replaces this candidate) then re-attempts the merge against fresh data, so a
    // provider-side fix resolves the collision without a FAR republish, and the last-good set keeps
    // serving meanwhile. A candidate whose CODE fails to compile never reaches this state: it is
    // dropped immediately (see RebuildFarCandidate) so a provider-side fix can never be asked to
    // resolve what is actually a broken policy, and an identical FAR re-send is retried rather than
    // silently suppressed.
    private Dictionary<string, PolicyContent>? _pendingContents;
    private Dictionary<string, PolicyLibraryModule>? _pendingLibraries;
    private byte[]? _pendingData;

    // The currently published snapshot. Guarded by _publishSync.
    private ImmutableArray<IPolicy> _current = [];
    private ImmutableArray<IObserver<ImmutableArray<IPolicy>>> _observers = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="RegoPolicyProvider"/>.
    /// </summary>
    public RegoPolicyProvider(IFusionExecutionDiagnosticEvents diagnosticEvents)
        : this(diagnosticEvents, dataAggregator: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RegoPolicyProvider"/> that merges data from
    /// registered <see cref="IRegoDataProvider"/> instances into the FAR data document before every
    /// compile.
    /// </summary>
    internal RegoPolicyProvider(
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        RegoDataAggregator? dataAggregator,
        Func<byte[], IReadOnlyList<PolicyModule>, IReadOnlyList<string>, CompiledPolicySet>? compiler = null)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _diagnosticEvents = diagnosticEvents;
        _dataAggregator = dataAggregator;
        _compiler = compiler ?? (static (data, modules, entryPoints) =>
            CompiledPolicySet.Compile(data, modules, entryPoints));

        if (_dataAggregator is not null)
        {
            _dataAggregator.DataChanged += OnProviderDataChanged;
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_publishSync)
        {
            if (_disposed)
            {
                observer.OnCompleted();
                return EmptySubscription.Instance;
            }

            observer.OnNext(_current);
            _observers = _observers.Add(observer);
        }

        return new Subscription(this, observer);
    }

    public void OnNext(PolicyContentSnapshot? content)
    {
        lock (_publishSync)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                Process(content);
            }
            catch (Exception ex)
            {
                _diagnosticEvents.PolicyUpdateError(ex);
            }
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    private void Process(PolicyContentSnapshot? content)
    {
        if (content is not { Language: RegoLanguage })
        {
            _contents = new Dictionary<string, PolicyContent>(StringComparer.Ordinal);
            _libraries = new Dictionary<string, PolicyLibraryModule>(StringComparer.Ordinal);
            _data = null;
            _lastMergedData = null;
            _pendingContents = null;
            _pendingLibraries = null;
            _pendingData = null;
            _currentHandle = null;
            Emit([]);
            return;
        }

        var codeChanged = content.Policies.Length != _contents.Count
            || content.Libraries.Length != _libraries.Count;
        var contents = new Dictionary<string, PolicyContent>(StringComparer.Ordinal);

        foreach (var policyContent in content.Policies)
        {
            contents[policyContent.Name] = policyContent;

            if (!codeChanged
                && (!_contents.TryGetValue(policyContent.Name, out var existing)
                    || !existing.Digest.Span.SequenceEqual(policyContent.Digest.Span)))
            {
                codeChanged = true;
            }
        }

        var libraries = new Dictionary<string, PolicyLibraryModule>(StringComparer.Ordinal);

        foreach (var library in content.Libraries)
        {
            libraries[library.Name] = library;

            if (!codeChanged
                && (!_libraries.TryGetValue(library.Name, out var existingLibrary)
                    || !existingLibrary.Digest.Span.SequenceEqual(library.Digest.Span)))
            {
                codeChanged = true;
            }
        }

        var data = content.Data.ToArray();
        var dataChanged = _data is null
            || !_data.AsSpan().SequenceEqual(data);

        if (!codeChanged && !dataChanged)
        {
            return;
        }

        RebuildFarCandidate(contents, libraries, data);
    }

    // Called when a registered data provider publishes a change. Two independent rebuild attempts
    // follow, both reported on: the currently served last-good FAR content is recompiled against
    // the freshest provider data (so the served set never goes stale just because a FAR candidate
    // happens to be stuck pending elsewhere), and, separately, whatever FAR candidate is currently
    // pending (a startup load still waiting on providers, or a merge collision) is retried against
    // the same fresh data.
    private void OnProviderDataChanged()
    {
        lock (_publishSync)
        {
            if (_disposed)
            {
                return;
            }

            if (_contents.Count > 0)
            {
                TryMergeAndCommit(_contents, _libraries, _data!, isPendingCandidate: false);
            }

            if (_pendingContents is not null)
            {
                TryMergeAndCommit(
                    _pendingContents,
                    _pendingLibraries ?? new Dictionary<string, PolicyLibraryModule>(StringComparer.Ordinal),
                    _pendingData!,
                    isPendingCandidate: true);
            }
        }
    }

    // Handles a new FAR candidate (a code and/or data change reported by the configuration
    // stream). Replaces whatever candidate was previously pending, exactly like a fresh FAR
    // publish always does regardless of what it replaces.
    private void RebuildFarCandidate(
        Dictionary<string, PolicyContent> contents,
        Dictionary<string, PolicyLibraryModule> libraries,
        byte[] data)
    {
        _pendingContents = contents;
        _pendingLibraries = libraries;
        _pendingData = data;

        if (contents.Count == 0)
        {
            _contents = contents;
            _libraries = libraries;
            _data = data;
            _lastMergedData = null;
            _pendingContents = null;
            _pendingLibraries = null;
            _pendingData = null;
            _currentHandle = null;
            Emit([]);
            return;
        }

        if (_dataAggregator is null)
        {
            CompileAndCommitOrDrop(contents, libraries, data, data);
            return;
        }

        // ORDER fix (F4): validate that the candidate's CODE compiles at all - against whatever
        // data last compiled successfully, or the candidate's own data if there is no last-good
        // yet, since there is nothing else to check it against - before it can ever become a
        // pending merge-collision candidate. A candidate whose code is broken is always dropped
        // here and reported, never left stuck waiting on a provider-side fix that could never
        // resolve a compile error.
        var precheckData = _lastMergedData ?? data;

        var precheckCompiled = TryCompile(
            contents,
            libraries,
            precheckData,
            out var precheckSet,
            out var precheckPolicies,
            out _,
            out var precheckError);

        // The precheck only needs to know whether the candidate's code compiles at all: the set
        // it produces is never served, so it is disposed immediately instead of being leaked.
        using var disposablePrecheckSet = precheckSet;

        if (!precheckCompiled)
        {
            ReportCompileFailure(precheckPolicies, precheckError!);
            _pendingContents = null;
            _pendingLibraries = null;
            _pendingData = null;
            return;
        }

        TryMergeAndCommit(contents, libraries, data, isPendingCandidate: true);
    }

    // The simple, no-data-aggregator path: a single compile attempt, committed on success and
    // dropped (with a diagnostic) on failure.
    private void CompileAndCommitOrDrop(
        Dictionary<string, PolicyContent> contents,
        Dictionary<string, PolicyLibraryModule> libraries,
        byte[] data,
        byte[] compileData)
    {
        if (!TryCompile(
            contents,
            libraries,
            compileData,
            out var set,
            out var policies,
            out var entryPoints,
            out var error))
        {
            ReportCompileFailure(policies, error!);
            _pendingContents = null;
            _pendingLibraries = null;
            _pendingData = null;
            return;
        }

        CommitCompiledSet(contents, libraries, data, compileData, set!, policies, entryPoints);
        _pendingContents = null;
        _pendingLibraries = null;
        _pendingData = null;
    }

    // Attempts to merge the given contents/data through the data aggregator and, on a clean merge,
    // compile and commit the result. The candidate's code is assumed to already compile (either
    // because RebuildFarCandidate just precheck-validated it, or because it is a retry of a
    // candidate that already did). isPendingCandidate controls whether _pendingContents/_pendingData
    // are cleared: true when the contents/data being attempted ARE the tracked pending candidate
    // (so a resolution or a fresh compile failure retires it), false when recompiling the currently
    // served last-good content on a provider refresh (which must never disturb an unrelated pending
    // candidate).
    private void TryMergeAndCommit(
        Dictionary<string, PolicyContent> contents,
        Dictionary<string, PolicyLibraryModule> libraries,
        byte[] data,
        bool isPendingCandidate)
    {
        switch (_dataAggregator!.TryBuildMergedData(data, out var attempt, out var mergeError))
        {
            case RegoDataMergeStatus.NotReady:
                // A registered provider has not completed its initial load yet: stay on the
                // existing "no data yet" path (nothing published) until it does.
                return;

            case RegoDataMergeStatus.Failed:
                // The candidate's data collides with the current provider data: keep it pending
                // (it is already known to compile) rather than dropping it, so a provider-side
                // fix or the next FAR publish (which replaces this candidate) retries the merge.
                // Every failed attempt is reported here, so the diagnostic is never suppressed.
                _diagnosticEvents.PolicyUpdateError(mergeError!);
                return;

            case RegoDataMergeStatus.Ready:
                if (!isPendingCandidate
                    && _lastMergedData is not null
                    && attempt!.MergedData.AsSpan().SequenceEqual(_lastMergedData))
                {
                    // Recompiling the currently served content: if the effective (FAR, providers)
                    // combination is byte-for-byte the same one already compiled and served (most
                    // commonly a colliding provider candidate that never promotes, so nothing
                    // about the served content actually changed), recompiling and republishing an
                    // identical policy set would be pure waste. The provider promotions this
                    // attempt staged are still committed here: the served set was compiled with
                    // exactly these merged bytes, so it is safe (and necessary) to advance the
                    // provider's last-good snapshot even though nothing is recompiled or republished.
                    // A pending candidate is never skipped here: it is by definition new content
                    // that has not been served yet.
                    attempt!.Commit();
                    return;
                }

                if (!TryCompile(
                    contents,
                    libraries,
                    attempt!.MergedData,
                    out var set,
                    out var policies,
                    out var entryPoints,
                    out var compileError))
                {
                    // Compiled fine against the last-good data but not against the actual merged
                    // data: drop it like any other compile failure. Nothing was committed, so the
                    // provider promotions this attempt staged are simply discarded - no provider's
                    // last-good snapshot is ever touched by a candidate that never got compiled.
                    ReportCompileFailure(policies, compileError!);

                    if (isPendingCandidate)
                    {
                        _pendingContents = null;
                        _pendingLibraries = null;
                        _pendingData = null;
                    }

                    return;
                }

                // Compile succeeded: only now does the aggregator commit the provider promotions
                // this attempt staged, atomically with this policy set becoming the served one.
                attempt.Commit();
                CommitCompiledSet(contents, libraries, data, attempt.MergedData, set!, policies, entryPoints);

                if (isPendingCandidate)
                {
                    _pendingContents = null;
                    _pendingLibraries = null;
                    _pendingData = null;
                }

                return;
        }
    }

    private bool TryCompile(
        Dictionary<string, PolicyContent> contents,
        Dictionary<string, PolicyLibraryModule> libraries,
        byte[] compileData,
        out CompiledPolicySet? set,
        out List<PolicyDefinition> policies,
        out List<string> entryPoints,
        out Exception? error)
    {
        policies = new List<PolicyDefinition>();
        var modules = new List<PolicyModule>(contents.Count + libraries.Count);

        foreach (var content in contents.Values)
        {
            var source = NormalizeSource(Encoding.UTF8.GetString(content.Source.Span));
            modules.Add(new PolicyModule(
                $"{content.Name}.rego",
                source));

            var rules = RegoEntrypointScanner.Scan(source);

            if (rules.Count == 0)
            {
                policies.Add(new PolicyDefinition(
                    $"{content.Name}.allow",
                    content.Name,
                    content.Requirements));
            }
            else
            {
                foreach (var rule in rules)
                {
                    policies.Add(new PolicyDefinition(
                        $"{content.Name}.{rule}",
                        content.Name,
                        content.Requirements));
                }
            }
        }

        // Shared library modules are compiled alongside every decision but are never scanned for
        // entrypoints: a library is never a decision and never falls back to a synthetic '.allow'.
        foreach (var library in libraries.Values)
        {
            modules.Add(new PolicyModule(
                library.Name,
                NormalizeSource(Encoding.UTF8.GetString(library.Source.Span))));
        }

        entryPoints = policies.Select(static p => $"data.{p.Name}").ToList();

        try
        {
            set = _compiler(compileData, modules, entryPoints);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            set = null;
            error = ex;
            return false;
        }
    }

    private void CommitCompiledSet(
        Dictionary<string, PolicyContent> contents,
        Dictionary<string, PolicyLibraryModule> libraries,
        byte[] data,
        byte[] mergedData,
        CompiledPolicySet set,
        List<PolicyDefinition> policies,
        List<string> entryPoints)
    {
        _contents = contents;
        _libraries = libraries;
        _data = data;
        _lastMergedData = mergedData;

        var handle = new PolicySetHandle(set);
        var compiledPolicies = ImmutableArray.CreateBuilder<IPolicy>(policies.Count);

        for (var i = 0; i < policies.Count; i++)
        {
            compiledPolicies.Add(new RegoPolicy(
                policies[i].Name,
                policies[i].Requirements,
                handle,
                set.GetEntryPointIndex(entryPoints[i])));
        }

        _currentHandle = handle;
        Emit(compiledPolicies.MoveToImmutable());
    }

    private void ReportCompileFailure(List<PolicyDefinition> policies, Exception error)
    {
        var reported = false;

        foreach (var policy in policies)
        {
            if (error.Message.Contains($"{policy.PairName}.rego:", StringComparison.Ordinal))
            {
                _diagnosticEvents.PolicyCompilationError(policy.Name, error);
                reported = true;
            }
        }

        if (!reported)
        {
            _diagnosticEvents.PolicyUpdateError(error);
        }
    }

    private void Emit(ImmutableArray<IPolicy> policies)
    {
        lock (_publishSync)
        {
            _current = policies;

            foreach (var observer in _observers)
            {
                observer.OnNext(policies);
            }
        }
    }

    private void Unsubscribe(IObserver<ImmutableArray<IPolicy>> observer)
    {
        lock (_publishSync)
        {
            _observers = _observers.Remove(observer);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (_publishSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _current = [];
            _observers = [];
            _currentHandle = null;
            _contents = new Dictionary<string, PolicyContent>(StringComparer.Ordinal);
            _libraries = new Dictionary<string, PolicyLibraryModule>(StringComparer.Ordinal);
            _data = null;
            _lastMergedData = null;
            _pendingContents = null;
            _pendingLibraries = null;
            _pendingData = null;
        }

        if (_dataAggregator is not null)
        {
            _dataAggregator.DataChanged -= OnProviderDataChanged;
            await _dataAggregator.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class Subscription(
        RegoPolicyProvider provider,
        IObserver<ImmutableArray<IPolicy>> observer)
        : IDisposable
    {
        public void Dispose() => provider.Unsubscribe(observer);
    }

    private sealed class EmptySubscription : IDisposable
    {
        public static EmptySubscription Instance { get; } = new();

        public void Dispose()
        {
        }
    }

    private sealed record PolicyDefinition(
        string Name,
        string PairName,
        PolicyRequirements Requirements);

    private static string NormalizeSource(string source)
        => source.StartsWith('\uFEFF') ? source[1..] : source;
}
