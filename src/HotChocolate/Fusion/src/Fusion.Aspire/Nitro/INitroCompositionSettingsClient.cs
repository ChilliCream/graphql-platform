namespace HotChocolate.Fusion.Aspire.Nitro;

internal interface INitroCompositionSettingsClient
{
    Task<CompositionSettings?> GetAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        CancellationToken cancellationToken);
}
