namespace StrawberryShake.CodeGeneration;

public static class TypeNames
{
    public const string IEntityStore = StrawberryShakeNamespace + "IEntityStore";
    public const string IEntityIdSerializer = StrawberryShakeNamespace + "IEntityIdSerializer";

    public const string IOperationRequestFactory =
        StrawberryShakeNamespace + "IOperationRequestFactory";

    public const string IStoreAccessor = StrawberryShakeNamespace + "IStoreAccessor";
    public const string StoreAccessor = StrawberryShakeNamespace + "StoreAccessor";

    public const string IEntityStoreSnapshot =
        StrawberryShakeNamespace + "IEntityStoreSnapshot";

    public const string IEntityStoreUpdateSession =
        StrawberryShakeNamespace + "IEntityStoreUpdateSession";

    public const string EntityStore = StrawberryShakeNamespace + "EntityStore";
    public const string IOperationStore = StrawberryShakeNamespace + "IOperationStore";
    public const string OperationStore = StrawberryShakeNamespace + "OperationStore";
    public const string IOperationExecutor = StrawberryShakeNamespace + "IOperationExecutor";
    public const string OperationExecutor = StrawberryShakeNamespace + "OperationExecutor";

    public const string StorelessOperationExecutor =
        StrawberryShakeNamespace + "StorelessOperationExecutor";

    public const string IOperationResult = StrawberryShakeNamespace + "IOperationResult";
    public const string OperationResult = StrawberryShakeNamespace + "OperationResult";

    public const string IOperationResultDataFactory =
        StrawberryShakeNamespace + "IOperationResultDataFactory";

    public const string IOperationResultDataInfo =
        StrawberryShakeNamespace + "IOperationResultDataInfo";

    public const string IOperationResultBuilder =
        StrawberryShakeNamespace + "IOperationResultBuilder";

    public const string OperationResultBuilder =
        StrawberryShakeNamespace + "OperationResultBuilder";

    public const string IResultPatcher =
        StrawberryShakeNamespace + "IResultPatcher";

    public const string JsonResultPatcher =
        StrawberryShakeNamespace + "Json.JsonResultPatcher";

    public const string ISerializerResolver =
        StrawberryShakeNamespace + "Serialization.ISerializerResolver";

    public const string SerializerResolver =
        StrawberryShakeNamespace + "Serialization.SerializerResolver";

    public const string ISerializer = StrawberryShakeNamespace + "Serialization.ISerializer";

    public const string ILeafValueParser =
        StrawberryShakeNamespace + "Serialization.ILeafValueParser";

    public const string IInputValueFormatter =
        StrawberryShakeNamespace + "Serialization.IInputValueFormatter";

    public const string IInputObjectFormatter =
        StrawberryShakeNamespace + "Serialization.IInputObjectFormatter";

    public const string IConnection = StrawberryShakeNamespace + "IConnection";
    public const string IEntityMapper = StrawberryShakeNamespace + "IEntityMapper";
    public const string IDocument = StrawberryShakeNamespace + "IDocument";

    public const string GraphQLClientException =
        StrawberryShakeNamespace + "GraphQLClientException";

    public const string IClientError = StrawberryShakeNamespace + "IClientError";
    public const string ClientError = StrawberryShakeNamespace + "ClientError";
    public const string DocumentHash = StrawberryShakeNamespace + "DocumentHash";
    public const string RequestStrategy = StrawberryShakeNamespace + "RequestStrategy";

    public const string OperationKind = StrawberryShakeNamespace + "OperationKind";
    public const string EntityId = StrawberryShakeNamespace + "EntityId";
    public const string EntityIdOrData = StrawberryShakeNamespace + "EntityIdOrData";

    public const string IEntityUpdateSession =
        StrawberryShakeNamespace + "IEntityUpdateSession";

    public const string IEntityUpdateSession_Version = "Version";

    public const string Execute = "ExecuteAsync";
    public const string Watch = "Watch";
    public const string Response = StrawberryShakeNamespace + "Response";
    public const string OperationRequest = StrawberryShakeNamespace + "OperationRequest";
    public const string ExecutionStrategy = StrawberryShakeNamespace + "ExecutionStrategy";

    public const string GetPropertyOrNull =
        StrawberryShakeNamespace + "Json.JsonElementExtensions.GetPropertyOrNull";

    public const string ContainsFragment =
        StrawberryShakeNamespace + "Json.JsonElementExtensions.ContainsFragment";

    public const string HttpConnection =
        StrawberryShakeNamespace + "Transport.Http.HttpConnection";

    public const string IHttpConnection =
        StrawberryShakeNamespace + "Transport.Http.IHttpConnection";

    public const string InMemoryConnection =
        StrawberryShakeNamespace + "Transport.InMemory.InMemoryConnection";

    public const string IInMemoryConnection =
        StrawberryShakeNamespace + "Transport.InMemory.IInMemoryConnection";

    public const string WebSocketConnection =
        StrawberryShakeNamespace + "Transport.WebSockets.WebSocketConnection";

    public const string IWebSocketConnection =
        StrawberryShakeNamespace + "Transport.WebSockets.IWebSocketConnection";

    public const string ISessionPool =
        StrawberryShakeNamespace + "Transport.WebSockets.ISessionPool";

    public const string IInMemoryClientFactory =
        StrawberryShakeNamespace + "Transport.InMemory.IInMemoryClientFactory";

    public const string AddProtocol = GlobalDependencyInjectionNamespace
        + "WebSocketClientFactoryServiceCollectionExtensions.AddProtocol";

    public const string GraphQLWebSocketProtocolFactory = StrawberryShakeNamespace
        + "Transport.WebSockets.Protocol.GraphQLWebSocketProtocolFactory";

    public const string SequenceEqual =
        StrawberryShakeNamespace + "Internal.ComparisonHelper.SequenceEqual";

    public const string IEquatable = "global::System.IEquatable";
    public const string Type = "global::System.Type";
    public const string Nullable = "global::System.Nullable";
    public const string JsonElement = "global::System.Text.Json.JsonElement";
    public const string JsonDocument = "global::System.Text.Json.JsonDocument";
    public const string JsonValueKind = "global::System.Text.Json.JsonValueKind";
    public const string JsonWriterOptions = "global::System.Text.Json.JsonWriterOptions";
    public const string Utf8JsonWriter = "global::System.Text.Json.Utf8JsonWriter";

    public const string ParseError =
        StrawberryShakeNamespace + "Json.JsonErrorParser.ParseErrors";

    public const string String = "global::System.String";
    public const string Byte = "global::System.Byte";
    public const string SByte = "global::System.SByte";
    public const string ByteArray = "global::System.Byte[]";
    public const string Array = "global::System.Array";
    public const string Int16 = "global::System.Int16";
    public const string Int32 = "global::System.Int32";
    public const string Int64 = "global::System.Int64";
    public const string UInt16 = "global::System.UInt16";
    public const string UInt32 = "global::System.UInt32";
    public const string UInt64 = "global::System.UInt64";
    public const string Single = "global::System.Single";
    public const string Double = "global::System.Double";
    public const string Decimal = "global::System.Decimal";
    public const string Uri = "global::System.Uri";
    public const string Boolean = "global::System.Boolean";
    public const string Object = "global::System.Object";
    public const string Guid = "global::System.Guid";
    public const string DateTime = "global::System.DateTime";
    public const string DateOnly = "global::System.DateOnly";
    public const string TimeOnly = "global::System.TimeOnly";
    public const string TimeSpan = "global::System.TimeSpan";
    public const string EncodingUtf8 = "global::System.Text.Encoding.UTF8";
    public const string List = GenericCollectionsNamespace + "List";
    public const string IEnumerable = GenericCollectionsNamespace + "IEnumerable";
    public const string Concat = "global::System.Linq.Enumerable.Concat";
    public const string IList = GenericCollectionsNamespace + "IList";

    public const string IReadOnlyCollection =
        GenericCollectionsNamespace + "IReadOnlyCollection";

    public const string IReadOnlyDictionary =
        GenericCollectionsNamespace + "IReadOnlyDictionary";

    public const string IReadOnlyList = GenericCollectionsNamespace + "IReadOnlyList";
    public const string HashSet = GenericCollectionsNamespace + "HashSet";
    public const string ISet = GenericCollectionsNamespace + "ISet";
    public const string IReadOnlySpan = "global::System.ReadOnlySpan";
    public const string DateTimeOffset = "global::System.DateTimeOffset";
    public const string OrdinalStringComparison = "global::System.StringComparison.Ordinal";
    public const string Func = "global::System.Func";
    public const string Task = "global::System.Threading.Tasks.Task";
    public const string IOperationObservable = "global::System.IObservable";
    public const string CancellationToken = "global::System.Threading.CancellationToken";
    public const string NotSupportedException = "global::System.NotSupportedException";
    public const string ArgumentNullException = "global::System.ArgumentNullException";
    public const string ArgumentException = "global::System.ArgumentException";

    public const string ArgumentOutOfRangeException =
        "global::System.ArgumentOutOfRangeException";

    public const string Exception = "global::System.Exception";

    public const string IServiceCollection =
        GlobalDependencyInjectionNamespace + "IServiceCollection";

    public const string IServiceProvider = "global::System.IServiceProvider";

    public const string ServiceCollection =
        GlobalDependencyInjectionNamespace + "ServiceCollection";

    public const string GetRequiredService =
        GlobalDependencyInjectionNamespace
        + "ServiceProviderServiceExtensions.GetRequiredService";

    public const string AddSingleton =
        GlobalDependencyInjectionNamespace
        + "ServiceCollectionServiceExtensions.AddSingleton";

    public const string BuildServiceProvider =
        GlobalDependencyInjectionNamespace
        + "ServiceCollectionContainerBuilderExtensions.BuildServiceProvider";

    public const string InjectAttribute =
        "global::Microsoft.AspNetCore.Components.InjectAttribute";

    public const string ParameterAttribute =
        "global::Microsoft.AspNetCore.Components.ParameterAttribute";

    public const string IHttpClientFactory = "global::System.Net.Http.IHttpClientFactory";
    public const string TryAddSingleton = DependencyInjectionExtensions + "TryAddSingleton";
    public const string GenericCollectionsNamespace = "global::System.Collections.Generic.";
    public const string StrawberryShakeNamespace = "global::StrawberryShake.";
    public const string Dictionary = "global::System.Collections.Generic.Dictionary";
    public const string KeyValuePair = "global::System.Collections.Generic.KeyValuePair";

    public const string GlobalDependencyInjectionNamespace =
        "global::Microsoft.Extensions.DependencyInjection.";

    public const string DependencyInjectionNamespace =
        "Microsoft.Extensions.DependencyInjection";

    public const string DependencyInjectionExtensions =
        GlobalDependencyInjectionNamespace
        + "Extensions.ServiceCollectionDescriptorExtensions.";

    public const string UseQuery = StrawberryShakeNamespace + "Razor." + nameof(UseQuery);

    public const string UseSubscription =
        StrawberryShakeNamespace + "Razor." + nameof(UseSubscription);

    public const string Upload = StrawberryShakeNamespace + nameof(Upload);

    public const string StringSerializer =
        StrawberryShakeNamespace + "Serialization.StringSerializer";

    public const string BooleanSerializer =
        StrawberryShakeNamespace + "Serialization.BooleanSerializer";

    public const string ByteSerializer =
        StrawberryShakeNamespace + "Serialization.ByteSerializer";

    public const string ShortSerializer =
        StrawberryShakeNamespace + "Serialization.ShortSerializer";

    public const string IntSerializer =
        StrawberryShakeNamespace + "Serialization.IntSerializer";

    public const string LongSerializer =
        StrawberryShakeNamespace + "Serialization.LongSerializer";

    public const string FloatSerializer =
        StrawberryShakeNamespace + "Serialization.FloatSerializer";

    public const string DecimalSerializer =
        StrawberryShakeNamespace + "Serialization.DecimalSerializer";

    public const string UriSerializer =
        StrawberryShakeNamespace + "Serialization.UriSerializer";

    public const string UrlSerializer =
        StrawberryShakeNamespace + "Serialization.UrlSerializer";

    public const string UUIDSerializer =
        StrawberryShakeNamespace + "Serialization.UUIDSerializer";

    public const string IdSerializer = StrawberryShakeNamespace + "Serialization.IdSerializer";

    public const string DateTimeSerializer =
        StrawberryShakeNamespace + "Serialization.DateTimeSerializer";

    public const string DateSerializer =
        StrawberryShakeNamespace + "Serialization.DateSerializer";

    public const string LocalDateSerializer =
        StrawberryShakeNamespace + "Serialization.LocalDateSerializer";

    public const string LocalDateTimeSerializer =
        StrawberryShakeNamespace + "Serialization.LocalDateTimeSerializer";

    public const string LocalTimeSerializer =
        StrawberryShakeNamespace + "Serialization.LocalTimeSerializer";

    public const string Base64StringSerializer =
        StrawberryShakeNamespace + "Serialization.Base64StringSerializer";

    public const string ByteArraySerializer =
        StrawberryShakeNamespace + "Serialization.ByteArraySerializer";

    public const string TimeSpanSerializer =
        StrawberryShakeNamespace + "Serialization.TimeSpanSerializer";

    public const string UnsignedByteSerializer =
        StrawberryShakeNamespace + "Serialization.UnsignedByteSerializer";

    public const string UnsignedIntSerializer =
        StrawberryShakeNamespace + "Serialization.UnsignedIntSerializer";

    public const string UnsignedLongSerializer =
        StrawberryShakeNamespace + "Serialization.UnsignedLongSerializer";

    public const string UnsignedShortSerializer =
        StrawberryShakeNamespace + "Serialization.UnsignedShortSerializer";

    public const string JsonSerializer =
        StrawberryShakeNamespace + "Serialization.JsonSerializer";

    public const string UploadSerializer =
        StrawberryShakeNamespace + "Serialization.UploadSerializer";

    public const string IClientBuilder = StrawberryShakeNamespace + "IClientBuilder";
    public const string ClientBuilder = StrawberryShakeNamespace + "ClientBuilder";

    public const string ArrayWriter = StrawberryShakeNamespace + "Internal.ArrayWriter";
}
