namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed class NitroSeedCoordinatorRegistry
{
    private readonly Lock _sync = new();
    private readonly Dictionary<NitroStageResource, Task<NitroSeedCoordinator>> _coordinators = [];
    private readonly Func<
        NitroStageResource,
        CancellationToken,
        Task<NitroSeedCoordinator>> _createCoordinator;

    public NitroSeedCoordinatorRegistry()
        : this(CreateProductionCoordinatorAsync)
    {
    }

    private NitroSeedCoordinatorRegistry(
        Func<
            NitroStageResource,
            CancellationToken,
            Task<NitroSeedCoordinator>> createCoordinator)
    {
        _createCoordinator = createCoordinator;
    }

    public async Task<NitroSeedCoordinator> GetAsync(
        NitroStageResource stage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stage);

        Task<NitroSeedCoordinator> coordinator;
        lock (_sync)
        {
            if (_coordinators.TryGetValue(stage, out var existing))
            {
                coordinator = existing;
            }
            else
            {
                coordinator = _createCoordinator(stage, CancellationToken.None);
                _coordinators.Add(stage, coordinator);
            }
        }

        try
        {
            return await coordinator.WaitAsync(cancellationToken);
        }
        catch when (coordinator.IsFaulted || coordinator.IsCanceled)
        {
            lock (_sync)
            {
                if (_coordinators.TryGetValue(stage, out var current)
                    && ReferenceEquals(current, coordinator))
                {
                    _coordinators.Remove(stage);
                }
            }

            throw;
        }
    }

    public void DeleteRunSeeds()
    {
        Task<NitroSeedCoordinator>[] coordinators;
        lock (_sync)
        {
            coordinators = [.. _coordinators.Values];
        }

        foreach (var coordinator in coordinators)
        {
            if (coordinator.IsCompletedSuccessfully)
            {
                coordinator.Result.DeleteRunSeeds();
            }
        }
    }

    internal static NitroSeedCoordinatorRegistry CreateForTests(
        NitroSeedCoordinator coordinator)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        return new NitroSeedCoordinatorRegistry((_, _) => Task.FromResult(coordinator));
    }

    private static async Task<NitroSeedCoordinator> CreateProductionCoordinatorAsync(
        NitroStageResource stage,
        CancellationToken cancellationToken)
    {
        var nitro = stage.Api.Nitro;
        var apiKey = nitro.ApiKey is null
            ? null
            : await nitro.ApiKey.GetValueAsync(cancellationToken);
        var environment = new NitroResourceEnvironment(
            nitro.CloudUrl,
            apiKey,
            SystemNitroEnvironment.Instance);
        var defaultApiUrl = nitro.CloudUrl is null
            ? NitroDefaults.ApiUrl
            : new Uri(nitro.CloudUrl, UriKind.Absolute);

        return NitroSeedCoordinator.CreateProduction(
            stage.StageName,
            environment,
            defaultApiUrl,
            nitro.SeedUpdates.AutoUpdate);
    }

    private sealed class NitroResourceEnvironment(
        string? cloudUrl,
        string? apiKey,
        INitroEnvironment fallback)
        : INitroEnvironment
    {
        public string? GetVariable(string name)
            => name switch
            {
                NitroEnvironmentVariables.CloudUrl when cloudUrl is not null => cloudUrl,
                NitroEnvironmentVariables.ApiKey when apiKey is not null => apiKey,
                _ => fallback.GetVariable(name)
            };
    }
}
