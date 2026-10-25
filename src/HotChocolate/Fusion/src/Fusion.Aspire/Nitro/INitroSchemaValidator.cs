namespace HotChocolate.Fusion.Aspire.Nitro;

internal interface INitroSchemaValidator
{
    Task<NitroSchemaValidationReport> ValidateAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        byte[] schema,
        string schemaHash,
        CancellationToken cancellationToken);
}
