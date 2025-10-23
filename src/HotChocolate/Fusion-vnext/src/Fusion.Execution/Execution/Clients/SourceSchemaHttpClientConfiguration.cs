using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the configuration for fetching data from a source schema over HTTP.
/// </summary>
public class SourceSchemaHttpClientConfiguration
    : ISourceSchemaClientConfiguration
{
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
    /// <param name="onSourceSchemaResult">
    /// The action to call after a <see cref="SourceSchemaResult"/> was materialized.
    /// </param>
    public SourceSchemaHttpClientConfiguration(
        string name,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? onSourceSchemaResult = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(baseAddress);

        Name = name;
        HttpClientName = name;
        BaseAddress = baseAddress;
        SupportedOperations = supportedOperations;
        OnBeforeSend = onBeforeSend;
        OnAfterReceive = onAfterReceive;
        OnSourceSchemaResult = onSourceSchemaResult;
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
    /// <param name="onSourceSchemaResult">
    /// The action to call after a <see cref="SourceSchemaResult"/> was materialized.
    /// </param>
    public SourceSchemaHttpClientConfiguration(
        string name,
        string httpClientName,
        Uri baseAddress,
        SupportedOperationType supportedOperations = SupportedOperationType.All,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? onSourceSchemaResult = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(httpClientName);
        ArgumentNullException.ThrowIfNull(baseAddress);

        Name = name;
        HttpClientName = httpClientName;
        BaseAddress = baseAddress;
        SupportedOperations = supportedOperations;
        OnBeforeSend = onBeforeSend;
        OnAfterReceive = onAfterReceive;
        OnSourceSchemaResult = onSourceSchemaResult;
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
    public Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? OnBeforeSend { get; }

    /// <summary>
    /// Called after the response is received.
    /// </summary>
    public Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? OnAfterReceive { get; }

    /// <summary>
    /// Called after a <see cref="SourceSchemaResult"/> was materialized.
    /// </summary>
    public Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? OnSourceSchemaResult { get; }
}
