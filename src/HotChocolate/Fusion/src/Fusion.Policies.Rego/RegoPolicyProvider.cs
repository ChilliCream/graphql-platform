using System.Collections.Immutable;
using System.Text;
using ChilliCream.Regorus;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;

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

    // The provider keeps only the handle it last published. An earlier handle is dropped on
    // rebuild, not retired into a list: the RegoPolicy instances built from it that are still
    // pinned by an in-flight request keep it reachable for as long as they need it, and the
    // compiled policy set's SafeHandle releases the native policy engine memory once nothing
    // references it any more.
    private PolicySetHandle? _currentHandle;

    // The last successfully compiled and published code/data pair. Compared against on every
    // incoming snapshot to detect a real change, and reused verbatim when a data provider change
    // alone triggers a recompile. Only ever assigned once a compile attempt with this exact pair
    // has succeeded, so a candidate that failed to compile is never mistaken for the current state.
    private Dictionary<string, PolicyContent> _contents = new(StringComparer.Ordinal);
    private byte[]? _data;

    // The candidate a rebuild attempt is currently working towards. Set at the start of every
    // rebuild attempt and retained only while it is still waiting on a data provider's initial
    // load, so a provider-driven retry (once every provider has finished loading) resumes with
    // the same FAR content the attempt was started with, rather than the last committed one. A
    // candidate that fails to merge or compile is cleared immediately: it never survives across a
    // failed attempt, so the next provider update recompiles the last committed pair against the
    // fresh data instead of re-reporting the same broken candidate.
    private Dictionary<string, PolicyContent>? _pendingContents;
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
        RegoDataAggregator? dataAggregator)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _diagnosticEvents = diagnosticEvents;
        _dataAggregator = dataAggregator;

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
            _data = null;
            _pendingContents = null;
            _pendingData = null;
            _currentHandle = null;
            Emit([]);
            return;
        }

        var codeChanged = content.Policies.Length != _contents.Count;
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

        var data = content.Data.ToArray();
        var dataChanged = _data is null
            || !_data.AsSpan().SequenceEqual(data);

        if (!codeChanged && !dataChanged)
        {
            return;
        }

        Rebuild(contents, data);
    }

    // Called when a registered data provider publishes a change: the FAR code and data are
    // unaffected, but the data merged in from providers is not, so whatever candidate is
    // currently pending (a startup load still waiting on providers) or, failing that, the last
    // committed pair is recompiled against the freshly merged data.
    private void OnProviderDataChanged()
    {
        lock (_publishSync)
        {
            if (_disposed)
            {
                return;
            }

            var contents = _pendingContents ?? (_contents.Count > 0 ? _contents : null);

            if (contents is null)
            {
                return;
            }

            var data = _pendingContents is not null ? _pendingData : _data;

            Rebuild(contents, data);
        }
    }

    // Stages the candidate and compiles it; the committed _contents/_data pair (against which the
    // next snapshot is compared for changes) is only ever assigned once compilation with this
    // exact candidate has succeeded, so a candidate that fails to compile is never mistaken for
    // the current state and an identical retry is attempted again rather than silently suppressed.
    private void Rebuild(Dictionary<string, PolicyContent> contents, byte[]? data)
    {
        _pendingContents = contents;
        _pendingData = data;

        if (contents.Count == 0)
        {
            _contents = contents;
            _data = data;
            _pendingContents = null;
            _pendingData = null;
            _currentHandle = null;
            Emit([]);
            return;
        }

        if (data is null)
        {
            return;
        }

        var compileData = data;

        if (_dataAggregator is not null)
        {
            switch (_dataAggregator.TryBuildMergedData(data, out var merged, out var mergeError))
            {
                case RegoDataMergeStatus.NotReady:
                    // A registered provider has not completed its initial load yet: stay on the
                    // existing "no data yet" path (nothing published) until it does.
                    return;

                case RegoDataMergeStatus.Failed:
                    _diagnosticEvents.PolicyUpdateError(mergeError!);
                    _pendingContents = null;
                    _pendingData = null;
                    return;

                case RegoDataMergeStatus.Ready:
                    compileData = merged!;
                    break;
            }
        }

        var policies = new List<PolicyDefinition>();
        var modules = new List<PolicyModule>(contents.Count);

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

        var entryPoints = policies.Select(static p => $"data.{p.Name}").ToList();

        CompiledPolicySet set;

        try
        {
            set = CompiledPolicySet.Compile(compileData, modules, entryPoints);
        }
        catch (Exception ex)
        {
            ReportCompileFailure(policies, ex);
            _pendingContents = null;
            _pendingData = null;
            return;
        }

        _contents = contents;
        _data = data;
        _pendingContents = null;
        _pendingData = null;

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
            _data = null;
            _pendingContents = null;
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
