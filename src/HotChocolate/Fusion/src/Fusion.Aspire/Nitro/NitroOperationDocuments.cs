using System.Text;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The GraphQL documents that the integration sends to the Nitro API. The documents ship as
/// embedded resources with stable operation names so they can be registered as persisted
/// operations.
/// </summary>
internal static class NitroOperationDocuments
{
    private const string ResourceNamespace = "HotChocolate.Fusion.Aspire.Nitro.Operations";
#if NITRO_PERSISTED_OPERATIONS
    private const string ResolveApiNameHashFile = "ResolveNitroApiName.graphql.sha256";
    private const string ValidateSchemaHashFile = "ValidateNitroSchema.graphql.sha256";
    private const string PollSchemaValidationHashFile = "PollNitroSchemaValidation.graphql.sha256";

    private static string? s_resolveApiNameHash;
    private static string? s_validateSchemaHash;
    private static string? s_pollSchemaValidationHash;
#else
    private const string ResolveApiNameFile = "ResolveNitroApiName.graphql";
    private const string ValidateSchemaFile = "ValidateNitroSchema.graphql";
    private const string PollSchemaValidationFile = "PollNitroSchemaValidation.graphql";

    private static string? s_resolveApiName;
    private static string? s_validateSchema;
    private static string? s_pollSchemaValidation;
#endif

    /// <summary>
    /// The operation name of the document that resolves the name of an api by its id.
    /// </summary>
    public const string ResolveApiNameOperationName = "ResolveNitroApiName";
    public const string ValidateSchemaOperationName = "ValidateNitroSchema";
    public const string PollSchemaValidationOperationName = "PollNitroSchemaValidation";

#if NITRO_PERSISTED_OPERATIONS
    /// <summary>
    /// Gets the persisted operation id of the document that resolves the name of an api by its id.
    /// </summary>
    public static string GetResolveApiNameOperationId()
        => s_resolveApiNameHash ??= ReadDocument(ResolveApiNameHashFile).Trim();

    public static string GetValidateSchemaOperationId()
        => s_validateSchemaHash ??= ReadDocument(ValidateSchemaHashFile).Trim();

    public static string GetPollSchemaValidationOperationId()
        => s_pollSchemaValidationHash ??= ReadDocument(PollSchemaValidationHashFile).Trim();
#else
    /// <summary>
    /// Gets the document that resolves the name of an api by its id.
    /// </summary>
    public static string GetResolveApiNameDocument()
        => s_resolveApiName ??= ReadDocument(ResolveApiNameFile);

    public static string GetValidateSchemaDocument()
        => s_validateSchema ??= ReadDocument(ValidateSchemaFile);

    public static string GetPollSchemaValidationDocument()
        => s_pollSchemaValidation ??= ReadDocument(PollSchemaValidationFile);
#endif

    private static string ReadDocument(string fileName)
    {
        var resourceName = $"{ResourceNamespace}.{fileName}";
        using var stream = typeof(NitroOperationDocuments).Assembly
            .GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"The embedded Nitro operation document '{resourceName}' was not found.");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
