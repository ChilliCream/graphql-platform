using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using ChilliCream.Regorus;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// Compiles the Rego policies and data carried by the Fusion configuration stream and publishes the
/// complete compiled policy set whenever it changes.
/// </summary>
public sealed class RegoPolicyProvider : IPolicyProvider
{
    private const string RegoLanguage = "rego";

#if NET9_0_OR_GREATER
    private readonly Lock _publishSync = new();
#else
    private readonly object _publishSync = new();
#endif
    private readonly Channel<ConfigurationEvent> _channel =
        Channel.CreateUnbounded<ConfigurationEvent>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IDisposable _subscription;

    // The provider keeps only the handle it last published. An earlier handle is dropped on
    // rebuild, not retired into a list: the RegoPolicy instances built from it that are still
    // pinned by an in-flight request keep it reachable for as long as they need it, and the
    // compiled policy set's SafeHandle releases the native policy engine memory once nothing
    // references it any more.
    private PolicySetHandle? _currentHandle;
    private int _processing;

    // Loop-only state. Only touched by the single active drainer at a time.
    private Dictionary<string, PolicyContent> _contents = new(StringComparer.Ordinal);
    private byte[]? _data;

    // The currently published snapshot. Guarded by _publishSync.
    private ImmutableArray<IPolicy> _current = [];
    private ImmutableArray<IObserver<ImmutableArray<IPolicy>>> _observers = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="RegoPolicyProvider"/>.
    /// </summary>
    public RegoPolicyProvider(
        IFusionConfigurationProvider configurationProvider,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(configurationProvider);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _diagnosticEvents = diagnosticEvents;
        _subscription = configurationProvider.Subscribe(new ConfigurationObserver(this));
    }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_publishSync)
        {
            Debug.Assert(!_disposed, "The provider is subscribed during schema creation only.");
            observer.OnNext(_current);
            _observers = _observers.Add(observer);
        }

        return new Subscription(this, observer);
    }

    private void Enqueue(ConfigurationEvent configuration)
    {
        if (_channel.Writer.TryWrite(configuration))
        {
            Drain();
        }
    }

    private void Drain()
    {
        while (true)
        {
            if (Interlocked.CompareExchange(ref _processing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                while (_channel.Reader.TryRead(out var configuration))
                {
                    ProcessSafe(configuration);
                }
            }
            finally
            {
                Volatile.Write(ref _processing, 0);
            }

            if (_channel.Reader.Count == 0)
            {
                return;
            }
        }
    }

    private void ProcessSafe(ConfigurationEvent configuration)
    {
        try
        {
            Process(configuration);
        }
        catch (Exception ex)
        {
            _diagnosticEvents.PolicyUpdateError(ex);
        }
    }

    private void Process(ConfigurationEvent configuration)
    {
        var codeChanged = configuration.Policies.Length != _contents.Count;
        var contents = new Dictionary<string, PolicyContent>(StringComparer.Ordinal);

        foreach (var content in configuration.Policies)
        {
            contents[content.Name] = content;

            if (!codeChanged
                && (!_contents.TryGetValue(content.Name, out var existing)
                    || !existing.Digest.Span.SequenceEqual(content.Digest.Span)))
            {
                codeChanged = true;
            }
        }

        var dataChanged = configuration.Data is null
            ? _data is not null
            : _data is null || !configuration.Data.AsSpan().SequenceEqual(_data);

        if (!codeChanged && !dataChanged)
        {
            return;
        }

        _contents = contents;
        _data = configuration.Data;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_contents.Count == 0)
        {
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

    private void OnConfiguration(FusionConfiguration configuration)
    {
        if (configuration.Policies is
            {
                Language: RegoLanguage
            } policies)
        {
            Enqueue(new ConfigurationEvent(
                policies.Policies,
                policies.Data.ToArray()));
        }
        else
        {
            Enqueue(new ConfigurationEvent([], null));
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
        }

        _subscription.Dispose();
        _channel.Writer.TryComplete();

        while (Interlocked.CompareExchange(ref _processing, 1, 0) != 0)
        {
            Thread.Yield();
        }

        while (_channel.Reader.TryRead(out _))
        {
        }

        _currentHandle?.Dispose();
        _currentHandle = null;
        _contents.Clear();
        _data = null;

        return ValueTask.CompletedTask;
    }

    private sealed record ConfigurationEvent(
        ImmutableArray<PolicyContent> Policies,
        byte[]? Data);

    private sealed class ConfigurationObserver(RegoPolicyProvider provider)
        : IObserver<FusionConfiguration>
    {
        public void OnNext(FusionConfiguration value) => provider.OnConfiguration(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private sealed class Subscription(
        RegoPolicyProvider provider,
        IObserver<ImmutableArray<IPolicy>> observer)
        : IDisposable
    {
        public void Dispose() => provider.Unsubscribe(observer);
    }
}
