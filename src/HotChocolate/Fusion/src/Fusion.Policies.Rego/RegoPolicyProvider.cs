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

    // The provider keeps only the handle it last published. An earlier handle is dropped on
    // rebuild, not retired into a list: the RegoPolicy instances built from it that are still
    // pinned by an in-flight request keep it reachable for as long as they need it, and the
    // compiled policy set's SafeHandle releases the native policy engine memory once nothing
    // references it any more.
    private PolicySetHandle? _currentHandle;
    private Dictionary<string, PolicyContent> _contents = new(StringComparer.Ordinal);
    private byte[]? _data;

    // The currently published snapshot. Guarded by _publishSync.
    private ImmutableArray<IPolicy> _current = [];
    private ImmutableArray<IObserver<ImmutableArray<IPolicy>>> _observers = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="RegoPolicyProvider"/>.
    /// </summary>
    public RegoPolicyProvider(IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _diagnosticEvents = diagnosticEvents;
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
            _contents.Clear();
            _data = null;
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

        _contents = contents;
        _data = data;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_contents.Count == 0)
        {
            _currentHandle = null;
            Emit([]);
            return;
        }

        if (_data is null)
        {
            return;
        }

        var names = new List<string>(_contents.Count);
        var requirements = new List<PolicyRequirements>(_contents.Count);
        var modules = new List<PolicyModule>(_contents.Count);
        var entryPoints = new List<string>(_contents.Count);

        foreach (var content in _contents.Values)
        {
            names.Add(content.Name);
            requirements.Add(content.Requirements);
            modules.Add(new PolicyModule(
                $"{content.Name}.rego",
                Encoding.UTF8.GetString(content.Source.Span)));
            entryPoints.Add($"data.{content.Name}");
        }

        CompiledPolicySet set;

        try
        {
            set = CompiledPolicySet.Compile(_data, modules, entryPoints);
        }
        catch (Exception ex)
        {
            ReportCompileFailure(names, ex);
            return;
        }

        var handle = new PolicySetHandle(set);
        var policies = ImmutableArray.CreateBuilder<IPolicy>(names.Count);

        for (var i = 0; i < names.Count; i++)
        {
            policies.Add(new RegoPolicy(
                names[i],
                requirements[i],
                handle,
                set.GetEntryPointIndex(entryPoints[i])));
        }

        _currentHandle = handle;
        Emit(policies.MoveToImmutable());
    }

    private void ReportCompileFailure(List<string> names, Exception error)
    {
        foreach (var name in names)
        {
            if (error.Message.Contains(name, StringComparison.Ordinal))
            {
                _diagnosticEvents.PolicyCompilationError(name, error);
                return;
            }
        }

        _diagnosticEvents.PolicyUpdateError(error);
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
    public ValueTask DisposeAsync()
    {
        lock (_publishSync)
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            _current = [];
            _observers = [];
            _currentHandle = null;
            _contents.Clear();
            _data = null;
        }

        return ValueTask.CompletedTask;
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
}
