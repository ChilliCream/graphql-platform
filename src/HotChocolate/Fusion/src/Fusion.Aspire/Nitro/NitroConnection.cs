namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Everything that is needed to talk to the Nitro API: where it is and how to authorize
/// against it.
/// </summary>
/// <param name="ApiUrl">
/// The normalized Nitro API base URL.
/// </param>
/// <param name="GraphQLEndpoint">
/// The GraphQL endpoint of the Nitro API.
/// </param>
/// <param name="Credential">
/// The credential that authorizes requests, or a credential of kind
/// <see cref="NitroCredentialKind.None"/> when the user is not signed in.
/// </param>
internal sealed record NitroConnection(
    Uri ApiUrl,
    Uri GraphQLEndpoint,
    NitroCredential Credential);
