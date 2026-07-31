namespace ChilliCream.Nitro.Aspire;

internal sealed class FusionPipelineSession : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<
        FusionDeploymentResource,
        FusionDeploymentSessionState> _deployments = new(
            ReferenceEqualityComparer.Instance);
    private readonly FusionPipelineMemoryLimits _memoryLimits;
    private readonly CancellationToken _cancellationToken;
    private readonly CancellationTokenRegistration _cancellationRegistration;
    private int _activeLeases;
    private bool _clearRequested;
    private bool _canceled;
    private bool _disposed;

    public FusionPipelineSession(
        CancellationToken cancellationToken = default,
        FusionPipelineMemoryLimits? memoryLimits = null)
    {
        _memoryLimits = memoryLimits ?? FusionPipelineMemoryLimits.Default;
        _cancellationToken = cancellationToken;
        _cancellationRegistration = cancellationToken.UnsafeRegister(
            static state => ((FusionPipelineSession)state!).Cancel(),
            this);
    }

    internal int DeploymentCount
    {
        get
        {
            lock (_sync)
            {
                return _deployments.Count;
            }
        }
    }

    public void SetAll(
        IReadOnlyList<(
            FusionDeploymentResource Deployment,
            FusionDeploymentSessionState State)> deployments)
    {
        ArgumentNullException.ThrowIfNull(deployments);

        var uniqueDeployments = new HashSet<FusionDeploymentResource>(
            ReferenceEqualityComparer.Instance);
        foreach (var (deployment, _) in deployments)
        {
            if (!uniqueDeployments.Add(deployment))
            {
                throw new InvalidOperationException(
                    $"Fusion deployment '{deployment.Name}' was downloaded more than once.");
            }
        }

        var totalSourceBytes = deployments.Sum(
            item => item.State.SourceArchiveBytes);
        if (totalSourceBytes > _memoryLimits.TotalSourceArchiveBytes)
        {
            throw new InvalidDataException(
                "The downloaded Fusion sources exceed the "
                + $"{_memoryLimits.TotalSourceArchiveBytes:N0}-byte "
                + "aggregate in-memory size limit.");
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_canceled)
            {
                DisposeStates(deployments.Select(item => item.State));
                throw new OperationCanceledException(_cancellationToken);
            }

            if (_clearRequested || _activeLeases > 0)
            {
                throw new InvalidOperationException(
                    "Fusion pipeline state cannot be replaced while it is in use.");
            }

            ClearCore();
            foreach (var (deployment, state) in deployments)
            {
                _deployments.Add(deployment, state);
            }
        }
    }

    public FusionPipelineSessionLease Acquire(
        FusionDeploymentResource deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment);

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_canceled)
            {
                throw new OperationCanceledException(_cancellationToken);
            }

            if (_clearRequested)
            {
                throw new InvalidOperationException(
                    "Fusion pipeline state is being cleared.");
            }

            if (!_deployments.TryGetValue(deployment, out var state))
            {
                throw new InvalidOperationException(
                    $"Fusion sources for deployment '{deployment.Name}' have not been downloaded.");
            }

            _activeLeases++;
            return new FusionPipelineSessionLease(this, state);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            RequestClear();
        }
    }

    private void Cancel()
    {
        lock (_sync)
        {
            _canceled = true;
            RequestClear();
        }
    }

    private void RequestClear()
    {
        _clearRequested = true;
        if (_activeLeases == 0)
        {
            ClearCore();
            if (!_disposed && !_canceled)
            {
                _clearRequested = false;
            }
        }
    }

    private void Release()
    {
        lock (_sync)
        {
            _activeLeases--;
            if (_activeLeases == 0 && _clearRequested)
            {
                ClearCore();
                if (!_disposed && !_canceled)
                {
                    _clearRequested = false;
                }
            }
        }
    }

    private void ClearCore()
    {
        DisposeStates(_deployments.Values);
        _deployments.Clear();
    }

    private static void DisposeStates(
        IEnumerable<FusionDeploymentSessionState> states)
    {
        foreach (var state in states)
        {
            state.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            RequestClear();
        }

        _cancellationRegistration.Dispose();
    }

    internal sealed class FusionPipelineSessionLease(
        FusionPipelineSession session,
        FusionDeploymentSessionState state)
        : IDisposable
    {
        private FusionPipelineSession? _session = session;

        public FusionDeploymentSessionState State { get; } = state;

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref _session, null);
            current?.Release();
        }
    }
}

internal sealed class FusionDeploymentSessionState(
    string tag,
    string cloudUrl,
    string apiId,
    IReadOnlyList<FusionSessionSourceIdentity> sourceIdentities)
    : IDisposable
{
    private readonly object _sync = new();
    private IReadOnlyList<FusionSessionSource>? _sources;
    private byte[]? _fusionArchive;
    private string? _compositionEnvironment;
    private string? _fusionArchiveSha256;
    private bool _disposed;

    public string Tag { get; } = tag;

    public string CloudUrl { get; } = cloudUrl;

    public string ApiId { get; } = apiId;

    public IReadOnlyList<FusionSessionSourceIdentity> SourceIdentities { get; } =
        sourceIdentities;

    public IReadOnlyList<FusionSessionSource> Sources
    {
        get
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _sources
                    ?? throw new InvalidOperationException(
                        "Fusion source archives have not been materialized.");
            }
        }
    }

    public long SourceArchiveBytes
    {
        get
        {
            lock (_sync)
            {
                return _sources?.Sum(
                    source => (long)source.Archive.Length) ?? 0;
            }
        }
    }

    public string? CompositionEnvironment
    {
        get
        {
            lock (_sync)
            {
                return _compositionEnvironment;
            }
        }
    }

    public byte[] FusionArchive
    {
        get
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _fusionArchive
                    ?? throw new InvalidOperationException(
                        "The downloaded Fusion sources have not been composed.");
            }
        }
    }

    public string? FusionArchiveSha256
    {
        get
        {
            lock (_sync)
            {
                return _fusionArchiveSha256;
            }
        }
    }

    public void SetSources(IReadOnlyList<FusionSessionSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        lock (_sync)
        {
            if (_disposed || _sources is not null)
            {
                foreach (var source in sources)
                {
                    Array.Clear(source.Archive);
                }

                ObjectDisposedException.ThrowIf(_disposed, this);
                throw new InvalidOperationException(
                    "Fusion source archives have already been materialized.");
            }

            _sources = sources;
        }
    }

    public void SetComposition(
        string compositionEnvironment,
        byte[] fusionArchive,
        string fusionArchiveSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compositionEnvironment);
        ArgumentNullException.ThrowIfNull(fusionArchive);
        ArgumentException.ThrowIfNullOrWhiteSpace(fusionArchiveSha256);

        lock (_sync)
        {
            if (_disposed)
            {
                Array.Clear(fusionArchive);
                throw new ObjectDisposedException(
                    nameof(FusionDeploymentSessionState));
            }

            if (_fusionArchive is not null)
            {
                Array.Clear(fusionArchive);
                throw new InvalidOperationException(
                    "The Fusion archive has already been composed.");
            }

            _compositionEnvironment = compositionEnvironment;
            _fusionArchive = fusionArchive;
            _fusionArchiveSha256 = fusionArchiveSha256;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_sources is not null)
            {
                foreach (var source in _sources)
                {
                    Array.Clear(source.Archive);
                }

                _sources = null;
            }

            if (_fusionArchive is not null)
            {
                Array.Clear(_fusionArchive);
                _fusionArchive = null;
            }

            _compositionEnvironment = null;
            _fusionArchiveSha256 = null;
        }
    }
}

internal sealed record FusionSessionSourceIdentity(
    string Name,
    string Version,
    string ContentSha256);

internal sealed record FusionSessionSource(
    string Name,
    string Version,
    byte[] Archive,
    string ContentSha256);
