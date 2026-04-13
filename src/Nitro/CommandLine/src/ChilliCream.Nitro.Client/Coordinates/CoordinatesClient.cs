namespace ChilliCream.Nitro.Client.Coordinates;

internal sealed class CoordinatesClient(IApiClient apiClient) : ICoordinatesClient
{
    public async Task<ICoordinateUsageQuery_ApiById_Stages?> GetCoordinateUsageAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.CoordinateUsageQuery.ExecuteAsync(
            apiId,
            coordinate,
            from,
            to,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        if (data.ApiById?.Stages is not { } stages)
        {
            return null;
        }

        foreach (var stage in stages)
        {
            if (string.Equals(stage.Name, stageName, StringComparison.Ordinal))
            {
                return stage;
            }
        }

        return null;
    }

    public async Task<ICoordinateClientsQuery_ApiById_Stages?> GetCoordinateClientsAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.CoordinateClientsQuery.ExecuteAsync(
            apiId,
            coordinate,
            from,
            to,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        if (data.ApiById?.Stages is not { } stages)
        {
            return null;
        }

        foreach (var stage in stages)
        {
            if (string.Equals(stage.Name, stageName, StringComparison.Ordinal))
            {
                return stage;
            }
        }

        return null;
    }

    public async Task<ICoordinateOperationsQuery_ApiById_Stages?> GetCoordinateOperationsAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.CoordinateOperationsQuery.ExecuteAsync(
            apiId,
            coordinate,
            from,
            to,
            100,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        if (data.ApiById?.Stages is not { } stages)
        {
            return null;
        }

        foreach (var stage in stages)
        {
            if (string.Equals(stage.Name, stageName, StringComparison.Ordinal))
            {
                return stage;
            }
        }

        return null;
    }

    public async Task<ICoordinateImpactQuery_ApiById_Stages?> GetCoordinateImpactAsync(
        string apiId,
        string stageName,
        string coordinate,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.CoordinateImpactQuery.ExecuteAsync(
            apiId,
            coordinate,
            from,
            to,
            100,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        if (data.ApiById?.Stages is not { } stages)
        {
            return null;
        }

        foreach (var stage in stages)
        {
            if (string.Equals(stage.Name, stageName, StringComparison.Ordinal))
            {
                return stage;
            }
        }

        return null;
    }

    public async Task<IUnusedCoordinatesQuery_ApiById_Stages?> GetUnusedCoordinatesAsync(
        string apiId,
        string stageName,
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<CoordinateKind>? kinds,
        bool? isDeprecated,
        int first,
        CancellationToken cancellationToken)
    {
        var result = await apiClient.UnusedCoordinatesQuery.ExecuteAsync(
            apiId,
            from,
            to,
            kinds,
            isDeprecated,
            first,
            cancellationToken);

        var data = OperationResultHelper.EnsureData(result);
        if (data.ApiById?.Stages is not { } stages)
        {
            return null;
        }

        foreach (var stage in stages)
        {
            if (string.Equals(stage.Name, stageName, StringComparison.Ordinal))
            {
                return stage;
            }
        }

        return null;
    }
}
