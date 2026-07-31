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
    private const string WatchSchemaValidationHashFile = "WatchNitroSchemaValidation.graphql.sha256";
    private const string GetStageVersionHashFile = "GetNitroStageVersion.graphql.sha256";
    private const string WatchStageHashFile = "WatchNitroStage.graphql.sha256";
    private const string UploadSourceSchemaHashFile = "UploadFusionSourceSchema.graphql.sha256";
    private const string BeginDeploymentHashFile = "BeginFusionDeployment.graphql.sha256";
    private const string ClaimDeploymentHashFile = "ClaimFusionDeployment.graphql.sha256";
    private const string ValidateDeploymentHashFile = "ValidateFusionDeployment.graphql.sha256";
    private const string CommitDeploymentHashFile = "CommitFusionDeployment.graphql.sha256";
    private const string ReleaseDeploymentHashFile = "ReleaseFusionDeployment.graphql.sha256";
    private const string WatchDeploymentHashFile = "WatchFusionDeployment.graphql.sha256";

    private static string? s_resolveApiNameHash;
    private static string? s_validateSchemaHash;
    private static string? s_watchSchemaValidationHash;
    private static string? s_getStageVersionHash;
    private static string? s_watchStageHash;
    private static string? s_uploadSourceSchemaHash;
    private static string? s_beginDeploymentHash;
    private static string? s_claimDeploymentHash;
    private static string? s_validateDeploymentHash;
    private static string? s_commitDeploymentHash;
    private static string? s_releaseDeploymentHash;
    private static string? s_watchDeploymentHash;
#else
    private const string ResolveApiNameFile = "ResolveNitroApiName.graphql";
    private const string ValidateSchemaFile = "ValidateNitroSchema.graphql";
    private const string WatchSchemaValidationFile = "WatchNitroSchemaValidation.graphql";
    private const string GetStageVersionFile = "GetNitroStageVersion.graphql";
    private const string WatchStageFile = "WatchNitroStage.graphql";
    private const string UploadSourceSchemaFile = "UploadFusionSourceSchema.graphql";
    private const string BeginDeploymentFile = "BeginFusionDeployment.graphql";
    private const string ClaimDeploymentFile = "ClaimFusionDeployment.graphql";
    private const string ValidateDeploymentFile = "ValidateFusionDeployment.graphql";
    private const string CommitDeploymentFile = "CommitFusionDeployment.graphql";
    private const string ReleaseDeploymentFile = "ReleaseFusionDeployment.graphql";
    private const string WatchDeploymentFile = "WatchFusionDeployment.graphql";

    private static string? s_resolveApiName;
    private static string? s_validateSchema;
    private static string? s_watchSchemaValidation;
    private static string? s_getStageVersion;
    private static string? s_watchStage;
    private static string? s_uploadSourceSchema;
    private static string? s_beginDeployment;
    private static string? s_claimDeployment;
    private static string? s_validateDeployment;
    private static string? s_commitDeployment;
    private static string? s_releaseDeployment;
    private static string? s_watchDeployment;
#endif

    /// <summary>
    /// The operation name of the document that resolves the name of an api by its id.
    /// </summary>
    public const string ResolveApiNameOperationName = "ResolveNitroApiName";
    public const string ValidateSchemaOperationName = "ValidateNitroSchema";
    public const string WatchSchemaValidationOperationName = "WatchNitroSchemaValidation";
    public const string GetStageVersionOperationName = "GetNitroStageVersion";
    public const string WatchStageOperationName = "WatchNitroStage";

    /// <summary>
    /// The operation name of the document that uploads an immutable source schema version.
    /// </summary>
    public const string UploadSourceSchemaOperationName = "UploadFusionSourceSchema";

    /// <summary>
    /// The operation name of the document that opens a publication request.
    /// </summary>
    public const string BeginDeploymentOperationName = "BeginFusionDeployment";

    /// <summary>
    /// The operation name of the document that claims the publication slot.
    /// </summary>
    public const string ClaimDeploymentOperationName = "ClaimFusionDeployment";

    /// <summary>
    /// The operation name of the document that validates a composed configuration.
    /// </summary>
    public const string ValidateDeploymentOperationName = "ValidateFusionDeployment";

    /// <summary>
    /// The operation name of the document that commits a composed configuration.
    /// </summary>
    public const string CommitDeploymentOperationName = "CommitFusionDeployment";

    /// <summary>
    /// The operation name of the document that releases the publication slot.
    /// </summary>
    public const string ReleaseDeploymentOperationName = "ReleaseFusionDeployment";

    /// <summary>
    /// The operation name of the document that observes the state of a publication request.
    /// </summary>
    public const string WatchDeploymentOperationName = "WatchFusionDeployment";

#if NITRO_PERSISTED_OPERATIONS
    /// <summary>
    /// Gets the persisted operation id of the document that resolves the name of an api by its id.
    /// </summary>
    public static string GetResolveApiNameOperationId()
        => s_resolveApiNameHash ??= ReadDocument(ResolveApiNameHashFile).Trim();

    public static string GetValidateSchemaOperationId()
        => s_validateSchemaHash ??= ReadDocument(ValidateSchemaHashFile).Trim();

    public static string GetWatchSchemaValidationOperationId()
        => s_watchSchemaValidationHash ??= ReadDocument(WatchSchemaValidationHashFile).Trim();

    public static string GetStageVersionOperationId()
        => s_getStageVersionHash ??= ReadDocument(GetStageVersionHashFile).Trim();

    public static string GetWatchStageOperationId()
        => s_watchStageHash ??= ReadDocument(WatchStageHashFile).Trim();

    public static string GetUploadSourceSchemaOperationId()
        => s_uploadSourceSchemaHash ??= ReadDocument(UploadSourceSchemaHashFile).Trim();

    public static string GetBeginDeploymentOperationId()
        => s_beginDeploymentHash ??= ReadDocument(BeginDeploymentHashFile).Trim();

    public static string GetClaimDeploymentOperationId()
        => s_claimDeploymentHash ??= ReadDocument(ClaimDeploymentHashFile).Trim();

    public static string GetValidateDeploymentOperationId()
        => s_validateDeploymentHash ??= ReadDocument(ValidateDeploymentHashFile).Trim();

    public static string GetCommitDeploymentOperationId()
        => s_commitDeploymentHash ??= ReadDocument(CommitDeploymentHashFile).Trim();

    public static string GetReleaseDeploymentOperationId()
        => s_releaseDeploymentHash ??= ReadDocument(ReleaseDeploymentHashFile).Trim();

    public static string GetWatchDeploymentOperationId()
        => s_watchDeploymentHash ??= ReadDocument(WatchDeploymentHashFile).Trim();
#else
    /// <summary>
    /// Gets the document that resolves the name of an api by its id.
    /// </summary>
    public static string GetResolveApiNameDocument()
        => s_resolveApiName ??= ReadDocument(ResolveApiNameFile);

    public static string GetValidateSchemaDocument()
        => s_validateSchema ??= ReadDocument(ValidateSchemaFile);

    public static string GetWatchSchemaValidationDocument()
        => s_watchSchemaValidation ??= ReadDocument(WatchSchemaValidationFile);

    public static string GetStageVersionDocument()
        => s_getStageVersion ??= ReadDocument(GetStageVersionFile);

    public static string GetWatchStageDocument()
        => s_watchStage ??= ReadDocument(WatchStageFile);

    public static string GetUploadSourceSchemaDocument()
        => s_uploadSourceSchema ??= ReadDocument(UploadSourceSchemaFile);

    public static string GetBeginDeploymentDocument()
        => s_beginDeployment ??= ReadDocument(BeginDeploymentFile);

    public static string GetClaimDeploymentDocument()
        => s_claimDeployment ??= ReadDocument(ClaimDeploymentFile);

    public static string GetValidateDeploymentDocument()
        => s_validateDeployment ??= ReadDocument(ValidateDeploymentFile);

    public static string GetCommitDeploymentDocument()
        => s_commitDeployment ??= ReadDocument(CommitDeploymentFile);

    public static string GetReleaseDeploymentDocument()
        => s_releaseDeployment ??= ReadDocument(ReleaseDeploymentFile);

    public static string GetWatchDeploymentDocument()
        => s_watchDeployment ??= ReadDocument(WatchDeploymentFile);
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
