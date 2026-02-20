using System.Collections.Immutable;
using System.Net.Http.Headers;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the configuration for fetching data from a source schema over HTTP.
/// </summary>
public class SourceSchemaHttpClientConfiguration
    : ISourceSchemaClientConfiguration
{
    public const string DefaultClientName = "fusion";

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
    /// <param name="batchingMode">
    /// The batching mode.
    /// </param>
    /// <param name="defaultAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a single, non-Subscription GraphQL request.
    /// </param>
    /// <param name="batchingAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a batching request.
    /// </param>
    /// <param name="subscriptionAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a subscription.
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
        SourceSchemaHttpClientBatchingMode batchingMode = SourceSchemaHttpClientBatchingMode.VariableBatching,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
        Action<OperationPlanContext, ExecutionNode, HttpRequestMessage>? onBeforeSend = null,
        Action<OperationPlanContext, ExecutionNode, HttpResponseMessage>? onAfterReceive = null,
        Action<OperationPlanContext, ExecutionNode, SourceSchemaResult>? onSourceSchemaResult = null)
        : this(
            name,
            DefaultClientName,
            baseAddress,
            supportedOperations,
            batchingMode,
            defaultAcceptHeaderValues,
            batchingAcceptHeaderValues,
            subscriptionAcceptHeaderValues,
            onBeforeSend,
            onAfterReceive,
            onSourceSchemaResult)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SourceSchemaHttpClientConfiguration"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the source schema.
    /// </param>
    /// <param name="httpClientName">
    /// The name of the http client.
    /// </param>
    /// <param name="baseAddress">
    /// The base address of the source schema.
    /// </param>
    /// <param name="supportedOperations">
    /// The supported operations.
    /// </param>
    /// <param name="batchingMode">
    /// The batching mode.
    /// </param>
    /// <param name="defaultAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a single, non-Subscription GraphQL request.
    /// </param>
    /// <param name="batchingAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a batching request.
    /// </param>
    /// <param name="subscriptionAcceptHeaderValues">
    /// The <c>Accept</c> header values sent in case of a subscription.
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
        SourceSchemaHttpClientBatchingMode batchingMode = SourceSchemaHttpClientBatchingMode.VariableBatching,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null,
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null,
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
        BatchingMode = batchingMode;

        DefaultAcceptHeaderValues = defaultAcceptHeaderValues ?? AcceptContentTypes.Default;

        if (batchingAcceptHeaderValues.HasValue)
        {
            BatchingAcceptHeaderValues = batchingAcceptHeaderValues.Value;
        }
        else
        {
            BatchingAcceptHeaderValues = batchingMode == SourceSchemaHttpClientBatchingMode.ApolloRequestBatching
                ? AcceptContentTypes.ApolloRequestBatching
                : AcceptContentTypes.VariableBatching;
        }

        SubscriptionAcceptHeaderValues = subscriptionAcceptHeaderValues ?? AcceptContentTypes.Subscription;

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
    /// Gets the preferred batching mode.
    /// </summary>
    public SourceSchemaHttpClientBatchingMode BatchingMode { get; }

    /// <summary>
    /// Gets the <c>Accept</c> header values sent in case of a single, non-Subscription GraphQL request.
    /// </summary>
    public ImmutableArray<MediaTypeWithQualityHeaderValue> DefaultAcceptHeaderValues { get; }

    /// <summary>
    /// Gets the <c>Accept</c> header values sent in case of a batching request.
    /// </summary>
    public ImmutableArray<MediaTypeWithQualityHeaderValue> BatchingAcceptHeaderValues { get; }

    /// <summary>
    /// Gets the <c>Accept</c> header values sent in case of a subscription.
    /// </summary>
    public ImmutableArray<MediaTypeWithQualityHeaderValue> SubscriptionAcceptHeaderValues { get; }

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

    private static class AcceptContentTypes
    {
        public static readonly ImmutableArray<MediaTypeWithQualityHeaderValue> Default =
        [
            new("application/graphql-response+json") { CharSet = "utf-8" },
            new("application/json") { CharSet = "utf-8" },
            new("application/jsonl") { CharSet = "utf-8" },
            new("text/event-stream") { CharSet = "utf-8" }
        ];

        public static ImmutableArray<MediaTypeWithQualityHeaderValue> VariableBatching { get; } =
        [
            new("application/jsonl") { CharSet = "utf-8" },
            new("text/event-stream") { CharSet = "utf-8" },
            new("application/graphql-response+json") { CharSet = "utf-8" },
            new("application/json") { CharSet = "utf-8" }
        ];

        public static readonly ImmutableArray<MediaTypeWithQualityHeaderValue> ApolloRequestBatching =
        [
            new("application/jsonl") { CharSet = "utf-8" },
            new("text/event-stream") { CharSet = "utf-8" },
            new("application/json") { CharSet = "utf-8" }
        ];

        public static ImmutableArray<MediaTypeWithQualityHeaderValue> Subscription { get; } =
        [
            new("application/jsonl") { CharSet = "utf-8" },
            new("text/event-stream") { CharSet = "utf-8" }
        ];
    }
}
