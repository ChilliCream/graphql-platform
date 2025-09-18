using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the configuration for fetching data from a source schema over HTTP.
/// </summary>
public class SourceSchemaHttpClientConfiguration
    : ISourceSchemaClientConfiguration
{
    private readonly Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? _onBeforeSend;
    private readonly Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? _onAfterReceive;

    /// <summary>
    /// Initializes a new instance of <see cref="SourceSchemaHttpClientConfiguration"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the source schema.
    /// </param>
    /// <param name="baseAddress">
    /// The base address of the source schema.
    /// </param>
    /// <param name="supportedOperations">
    /// The supported operations.
    /// </param>
    /// <param name="onBeforeSend">
    /// The action to call before the request is sent.
    /// </param>
    /// <param name="onAfterReceive">
    /// The action to call after the response is received.
    /// </param>
    public SourceSchemaHttpClientConfiguration(
        string name,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(baseAddress);

        Name = name;
        HttpClientName = name;
        BaseAddress = baseAddress;
        SupportedOperations = supportedOperations;
        _onBeforeSend = onBeforeSend;
        _onAfterReceive = onAfterReceive;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SourceSchemaHttpClientConfiguration"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="httpClientName">
    /// The name of the http client.
    /// </param>
    /// <param name="baseAddress">
    /// The base address of the source schema.
    /// </param>
    /// <param name="supportedOperations">
    /// The supported operations.
    /// </param>
    /// <param name="onBeforeSend">
    /// The action to call before the request is sent.
    /// </param>
    /// <param name="onAfterReceive">
    /// The action to call after the response is received.
    /// </param>
    public SourceSchemaHttpClientConfiguration(
        string name,
        string httpClientName,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(httpClientName);
        ArgumentNullException.ThrowIfNull(baseAddress);

        Name = name;
        HttpClientName = httpClientName;
        BaseAddress = baseAddress;
        SupportedOperations = supportedOperations;
        _onBeforeSend = onBeforeSend;
        _onAfterReceive = onAfterReceive;
    }

    /// <summary>
    /// Gets the name of the source schema.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name of the underlying http client.
    /// </summary>
    public string HttpClientName { get; }

    /// <summary>
    /// Gets the base address of the source schema.
    /// </summary>
    public Uri BaseAddress { get; }

    /// <summary>
    /// Gets the supported operations.
    /// </summary>
    public SupportedOperationType SupportedOperations { get; }

    /// <summary>
    /// Called before the request is sent.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node triggering the request.
    /// </param>
    /// <param name="requestMessage">
    /// The request message.
    /// </param>
    public virtual void OnBeforeSend(
        OperationPlanContext context,
        ExecutionNode node,
        HttpRequestMessage requestMessage)
        => _onBeforeSend?.Invoke(context, node, requestMessage);

    /// <summary>
    /// Called after the response is received.
    /// </summary>
    /// <param name="context">
    /// The operation plan context.
    /// </param>
    /// <param name="node">
    /// The execution node triggering the request.
    /// </param>
    /// <param name="responseMessage">
    /// The response message.
    /// </param>
    public virtual void OnAfterReceive(
        OperationPlanContext context,
        ExecutionNode node,
        HttpResponseMessage responseMessage)
        => _onAfterReceive?.Invoke(context, node, responseMessage);
}
