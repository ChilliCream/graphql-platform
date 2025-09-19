namespace ChilliCream.Nitro.CommandLine.Cloud;

internal interface IConfigurationService
{
    Task<T?> GetAsync<T>(CancellationToken cancellationToken) where T : IConfigurationFile;

    Task SaveAsync<T>(T configuration, CancellationToken cancellationToken)
        where T : IConfigurationFile;

    Task ResetAsync<T>(CancellationToken cancellationToken) where T : IConfigurationFile;
}
