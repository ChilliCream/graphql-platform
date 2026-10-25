using System.Net.Http.Headers;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The request headers that the Nitro API expects. The header names match the Nitro CLI, so the
/// API sees the same shape from both clients.
/// </summary>
internal static class NitroRequestHeaders
{
    /// <summary>
    /// Carries an API key credential.
    /// </summary>
    public const string ApiKey = "CCC-api-key";

    /// <summary>
    /// Identifies the client that sends the request.
    /// </summary>
    public const string Agent = "ccc-agent";

    /// <summary>
    /// Carries the version of the client, used for persisted operation enforcement.
    /// </summary>
    public const string ClientVersion = "GraphQL-Client-Version";

    private const string AgentName = "HotChocolate.Fusion.Aspire";

    private static readonly string s_version = GetVersion();

    /// <summary>
    /// Gets the value of the <c>GraphQL-Client-Version</c> header.
    /// </summary>
    public static string ClientVersionValue => s_version;

    /// <summary>
    /// Gets the value of the <c>ccc-agent</c> header.
    /// </summary>
    public static string AgentValue { get; } = $"{AgentName}/{s_version}";

    /// <summary>
    /// Adds the client identification headers and the credential header to a request.
    /// </summary>
    /// <param name="requestMessage">
    /// The request that the headers are added to.
    /// </param>
    /// <param name="credential">
    /// The credential that authorizes the request. A credential of kind
    /// <see cref="NitroCredentialKind.None"/> adds no credential header.
    /// </param>
    public static void Apply(HttpRequestMessage requestMessage, NitroCredential credential)
    {
        ArgumentNullException.ThrowIfNull(requestMessage);
        ArgumentNullException.ThrowIfNull(credential);

        requestMessage.Headers.TryAddWithoutValidation(Agent, AgentValue);
        requestMessage.Headers.TryAddWithoutValidation(ClientVersion, ClientVersionValue);

        switch (credential.Kind)
        {
            case NitroCredentialKind.ApiKey:
                requestMessage.Headers.TryAddWithoutValidation(ApiKey, credential.Value);
                break;

            case NitroCredentialKind.AccessToken:
                requestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", credential.Value);
                break;
        }
    }

    private static string GetVersion()
    {
        var version = typeof(NitroRequestHeaders).Assembly.GetName().Version;

        return version is null
            ? "0.0.0"
            : new Version(version.Major, version.Minor, version.Build).ToString();
    }
}
