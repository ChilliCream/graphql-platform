using System.Buffers;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Language;
using HotChocolate.Utilities;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Configuration;

public class FileSystemFusionConfigurationProvider : IFusionConfigurationProvider
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
#if NET9_0_OR_GREATER
    private readonly Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif
    private readonly string _fileName;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly FileSystemWatcher _watcher;

    private readonly Channel<bool> _schemaUpdateEvents =
        Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropNewest,
                SingleReader = true,
                SingleWriter = false
            });

    private readonly CancellationTokenSource _cts;
    private ImmutableArray<ObserverSession> _sessions = [];
    private readonly bool _isPackage;
    private ulong _schemaDocumentHash;
    private ulong _settingsHash;
    private ulong _policyContentHash;
    private ulong _packageHash;
    private bool _disposed;

    public FileSystemFusionConfigurationProvider(
        string fileName,
        IFusionExecutionDiagnosticEvents? diagnosticEvents)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var fullPath = IOPath.GetFullPath(fileName);
        var directory = IOPath.GetDirectoryName(fullPath);

        _fileName = fullPath;
        _diagnosticEvents = diagnosticEvents ?? NoopFusionExecutionDiagnosticEvents.Instance;

        if (directory is null)
        {
            throw new FileNotFoundException("The file must contain a path.", fileName);
        }

        _isPackage = IOPath.GetExtension(fileName)?.ToLowerInvariant() is ".far";
        _cts = new CancellationTokenSource();

        _watcher = new FileSystemWatcher
        {
            Path = directory,
            Filter = "*.*",
            NotifyFilter =
                NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.Attributes
                | NotifyFilters.CreationTime
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
        };

        _watcher.Created += (_, e) =>
        {
            if (fullPath.Equals(e.FullPath, StringComparison.Ordinal))
            {
                _schemaUpdateEvents.Writer.TryWrite(true);
            }
        };

        _watcher.Changed += (_, e) =>
        {
            if (fullPath.Equals(e.FullPath, StringComparison.Ordinal))
            {
                _schemaUpdateEvents.Writer.TryWrite(true);
            }
        };

        _watcher.EnableRaisingEvents = true;
        _schemaUpdateEvents.Writer.TryWrite(true);

        SchemaUpdateProcessorAsync(_cts.Token).FireAndForget();
    }

    public FusionConfiguration? Configuration { get; private set; }

    public IDisposable Subscribe(IObserver<FusionConfiguration> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var session = new ObserverSession(this, observer);

        lock (_syncRoot)
        {
            _sessions = _sessions.Add(session);

            // The replay is delivered while the lock is held so that it is ordered with respect to a
            // concurrent NotifyObservers. Delivering it outside the lock would allow a newer
            // configuration to reach the new subscriber before this replayed value, reverting it to
            // stale content.
            if (Configuration is not null)
            {
                observer.OnNext(Configuration);
            }
        }

        return session;
    }

    private void Unsubscribe(ObserverSession session)
    {
        lock (_syncRoot)
        {
            _sessions = _sessions.Remove(session);
        }
    }

    private async Task SchemaUpdateProcessorAsync(CancellationToken ct)
    {
        var defaultSettings = JsonDocument.Parse("{ }");
        var defaultSettingsHash = XxHash64.HashToUInt64(JsonMarshal.GetRawUtf8Value(defaultSettings.RootElement));

        await foreach (var _ in _schemaUpdateEvents.Reader.ReadAllAsync(ct))
        {
            try
            {
                var settings = new JsonDocumentOwner(defaultSettings, EmptyMemoryOwner.Instance);
                PolicyContentSnapshot? policyContent = null;
                DocumentNode schema;
                ulong settingsHash;
                ulong schemaHash;
                ulong policyContentHash = 0;
                var packageHash = 0UL;

                if (_isPackage)
                {
                    await using (var fileStream = File.OpenRead(_fileName))
                    {
                        var hash = new XxHash64();
                        await hash.AppendAsync(fileStream, ct);
                        packageHash = hash.GetCurrentHashAsUInt64();

                        if (packageHash == _packageHash)
                        {
                            continue;
                        }
                    }

                    using var archive = FusionArchive.Open(_fileName);
                    using var config = await archive.TryGetGatewayConfigurationAsync(WellKnownVersions.LatestGatewayFormatVersion, ct);

                    if (config is null)
                    {
                        // ignore and wait for next update
                        continue;
                    }

                    await using var stream = await config.OpenReadSchemaAsync(ct);
                    (schema, schemaHash) = await ReadSchemaDocumentAsync(stream, ct);
                    var settingsSpan = JsonMarshal.GetRawUtf8Value(config.Settings.RootElement);
                    var buffer = new PooledArrayWriter(settingsSpan.Length);
                    buffer.Write(settingsSpan);
                    settingsHash = XxHash64.HashToUInt64(settingsSpan);
                    settings = new JsonDocumentOwner(JsonDocument.Parse(buffer.WrittenMemory), buffer);

                    policyContent = await PackagePolicyContentReader.ReadAsync(
                        archive,
                        WellKnownVersions.LatestRegoPolicyFormatVersion,
                        ct);
                    policyContentHash = ComputePolicyContentHash(policyContent);

                    // The schema, settings, and policy content were all read and parsed
                    // successfully, so the bytes behind this hash are known to be readable.
                    // Committing the hash any earlier would mean a malformed archive permanently
                    // suppresses reprocessing of the same bytes, since the equality check above
                    // would then treat them as already handled.
                    _packageHash = packageHash;
                }
                else
                {
                    await using var stream = File.OpenRead(_fileName);
                    (schema, schemaHash) = await ReadSchemaDocumentAsync(stream, ct);
                    settingsHash = defaultSettingsHash;
                }

                if (_schemaDocumentHash == schemaHash
                    && _settingsHash == settingsHash
                    && _policyContentHash == policyContentHash)
                {
                    settings.Dispose();
                    policyContent?.Dispose();
                    continue;
                }

                _settingsHash = settingsHash;
                _schemaDocumentHash = schemaHash;
                _policyContentHash = policyContentHash;
                NotifyObservers(new FusionConfiguration(schema, settings) { Policies = policyContent });
            }
            catch (Exception ex)
            {
                // surface the failure and wait for next update
                _diagnosticEvents.ConfigurationReadError(ex);
            }
        }
    }

    private async ValueTask<(DocumentNode, ulong)> ReadSchemaDocumentAsync(Stream stream, CancellationToken ct)
    {
        using var buffer = new PooledArrayWriter();
        var pipeReader = PipeReader.Create(stream);

        while (true)
        {
            var result = await pipeReader.ReadAsync(ct).ConfigureAwait(false);
            var readBuffer = result.Buffer;

            foreach (var segment in readBuffer)
            {
                var span = segment.Span;
                span.CopyTo(buffer.GetSpan(span.Length));
                buffer.Advance(span.Length);
            }

            pipeReader.AdvanceTo(readBuffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await pipeReader.CompleteAsync().ConfigureAwait(false);

        var hash = XxHash64.HashToUInt64(buffer.WrittenSpan);
        var document = Utf8GraphQLParser.Parse(buffer.WrittenSpan);
        return (document, hash);
    }

    private static ulong ComputePolicyContentHash(PolicyContentSnapshot? policyContent)
    {
        if (policyContent is null)
        {
            return 0;
        }

        var hash = new XxHash64();

        foreach (var policy in policyContent.Policies)
        {
            hash.Append(s_utf8Encoding.GetBytes(policy.Name));
            hash.Append(policy.Digest.Span);
        }

        hash.Append(policyContent.DataDigest.Span);
        return hash.GetCurrentHashAsUInt64();
    }

    private void NotifyObservers(FusionConfiguration configuration)
    {
        ImmutableArray<ObserverSession> sessions;

        lock (_syncRoot)
        {
            sessions = _sessions;
            Configuration = configuration;
        }

        if (sessions.IsEmpty)
        {
            return;
        }

        foreach (var session in sessions)
        {
            session.Notify(configuration);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();

        _cts.Cancel();
        _cts.Dispose();

        foreach (var session in _sessions)
        {
            session.Complete();
        }

        // drain events
        while (_schemaUpdateEvents.Reader.TryRead(out _))
        {
        }

        return ValueTask.CompletedTask;
    }

    private sealed class ObserverSession(
        FileSystemFusionConfigurationProvider provider,
        IObserver<FusionConfiguration> observer)
        : IDisposable
    {
        public void Notify(FusionConfiguration schemaDocument)
        {
            try
            {
                observer.OnNext(schemaDocument);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        public void Complete()
        {
            try
            {
                observer.OnCompleted();
            }
            catch
            {
                // We do not want to throw an exception if the observer
                // throws an exception on completion.
            }
        }

        public void Dispose()
            => provider.Unsubscribe(this);
    }

    private sealed class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public static readonly EmptyMemoryOwner Instance = new();

        public Memory<byte> Memory => default;

        public void Dispose()
        {
        }
    }
}
