using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.Client.Clients;

/// <summary>
/// Provides remote client-version operations used by client commands.
/// </summary>
public interface IClientsClient
{
    /// <summary>
    /// Creates a client for an API.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or payload errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ICreateClientCommandMutation_CreateClient> CreateClientAsync(
        string apiId,
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a client.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IDeleteClientByIdCommandMutation_DeleteClientById> DeleteClientAsync(
        string clientId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads client details.
    /// </summary>
    /// <returns>The client details, or <c>null</c> if no client was found.</returns>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IShowClientCommandQuery_Node?> ShowClientAsync(
        string clientId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists clients for an API.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>> ListClientsAsync(
        string apiId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists versions of a client.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<ConnectionPage<IClientDetailPrompt_ClientVersionEdge>> ListClientVersionsAsync(
        string clientId,
        string? after,
        int? first,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a client version.
    /// </summary>
    /// <returns>The uploaded client version.</returns>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or payload errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IUploadClient_UploadClient> UploadClientVersionAsync(
        string clientId,
        string tag,
        Stream operationsStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a client validation request.
    /// </summary>
    /// <returns>The validation request.</returns>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or payload errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IValidateClientVersion_ValidateClient> StartClientValidationAsync(
        string clientId,
        string stageName,
        Stream operationsStream,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams client validation updates for a request.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The subscription failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    IAsyncEnumerable<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate> SubscribeToClientValidationAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a client publish request.
    /// </summary>
    /// <returns>The publish request.</returns>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or payload errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IPublishClientVersion_PublishClient> StartClientPublishAsync(
        string clientId,
        string stageName,
        string tag,
        bool force,
        bool waitForApproval,
        SourceMetadata? source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Streams client publish updates for a request.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The subscription failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    IAsyncEnumerable<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate> SubscribeToClientPublishAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublishes a client version by tag from a stage.
    /// </summary>
    /// <exception cref="NitroClientException">
    /// The request failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<IUnpublishClient_UnpublishClient> UnpublishClientVersionAsync(
        string clientId,
        string stageName,
        string tag,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads persisted queries from a stage.
    /// </summary>
    /// <returns>The query stream, or <c>null</c> if no published client exists on the stage.</returns>
    /// <remarks>The caller owns and must dispose the returned stream.</remarks>
    /// <exception cref="NitroClientAuthorizationException">
    /// The request was rejected because the current credentials do not grant access.
    /// </exception>
    /// <exception cref="NitroClientException">
    /// The download failed due to transport or server-side errors.
    /// </exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    Task<Stream?> DownloadPersistedQueriesAsync(
        string apiId,
        string stageName,
        CancellationToken cancellationToken);
}
