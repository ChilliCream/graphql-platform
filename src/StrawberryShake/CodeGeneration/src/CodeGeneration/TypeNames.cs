namespace StrawberryShake.CodeGeneration
{
    public static class TypeNames
    {
        public const string IEntityStore = StrawberryshakeNamespace + "IEntityStore";
        public const string EntityStore = StrawberryshakeNamespace + "EntityStore";
        public const string IOperationStore = StrawberryshakeNamespace + "IOperationStore";
        public const string OperationStore = StrawberryshakeNamespace + "OperationStore";
        public const string IOperationExecutor = StrawberryshakeNamespace + "IOperationExecutor";
        public const string OperationExecutor = StrawberryshakeNamespace + "OperationExecutor";
        public const string IOperationResult = StrawberryshakeNamespace + "IOperationResult";
        public const string OperationResult = StrawberryshakeNamespace + "OperationResult";
        public const string IOperationResultDataFactory = StrawberryshakeNamespace + "IOperationResultDataFactory";
        public const string IOperationResultDataInfo = StrawberryshakeNamespace + "IOperationResultDataInfo";
        public const string IOperationResultBuilder = StrawberryshakeNamespace + "IOperationResultBuilder";
        public const string ISerializerResolver = StrawberryshakeNamespace + "Serialization.ISerializerResolver";
        public const string SerializerResolver = StrawberryshakeNamespace + "Serialization.SerializerResolver";
        public const string ISerializer = StrawberryshakeNamespace + "Serialization.ISerializer";
        public const string ILeafValueParser = StrawberryshakeNamespace + "Serialization.ILeafValueParser";
        public const string IInputValueFormatter = StrawberryshakeNamespace + "Serialization.IInputValueFormatter ";
        public const string IConnection = StrawberryshakeNamespace + "IConnection";
        public const string IEntityMapper = StrawberryshakeNamespace + "IEntityMapper";
        public const string IDocument = StrawberryshakeNamespace + "IDocument";
        public const string IGraphQLClientException = StrawberryshakeNamespace + "GraphQLClientException";

        public const string OperationKind = StrawberryshakeNamespace + "OperationKind";
        public const string EntityId = StrawberryshakeNamespace + "EntityId";
        public const string IEntityUpdateSession = StrawberryshakeNamespace + "IEntityUpdateSession";
        public const string IEntityUpdateSession_Version = "Version";

        public const string Execute = "ExecuteAsync";
        public const string Watch = "Watch";
        public const string Response = StrawberryshakeNamespace + "Response";
        public const string OperationRequest = StrawberryshakeNamespace + "OperationRequest";
        public const string ExecutionStrategy = StrawberryshakeNamespace + "ExecutionStrategy";
        public const string GetPropertyOrNull = StrawberryshakeNamespace + "Transport.Http.JsonElementExtensions.GetPropertyOrNull";
        public const string HttpConnection= StrawberryshakeNamespace + "Transport.Http.HttpConnection";
        public const string WebSocketConnection= StrawberryshakeNamespace + "Transport.WebSockets.WebSocketConnection";
        public const string ISessionPool= StrawberryshakeNamespace + "Transport.WebSockets.ISessionPool";
        public const string AddProtocol= DependencyInjectionNamepsace + "WebSocketClientFactoryServiceCollectionExtensions.AddProtocol";
        public const string GraphQLWebSocketProtocolFactory= StrawberryshakeNamespace + "Transport.WebSockets.Protocol.GraphQLWebSocketProtocolFactory";

        public const string JsonElement = "global::System.Text.Json.JsonElement";
        public const string JsonDocument = "global::System.Text.Json.JsonDocument";
        public const string String = "global::System.String";
        public const string Byte = "global::System.Byte";
        public const string ByteArray = "global::System.Byte[]";
        public const string Int16 = "global::System.Int16";
        public const string Int32 = "global::System.Int32";
        public const string Int64 = "global::System.Int64";
        public const string Double = "global::System.Double";
        public const string Decimal = "global::System.Decimal";
        public const string Uri = "global::System.Uri";
        public const string Boolean = "global::System.Boolean";
        public const string Object = "global::System.Object";
        public const string Guid = "global::System.Guid";
        public const string DateTime = "global::System.DateTime";
        public const string EncodingUtf8 = "global::System.Text.Encoding.UTF8";
        public const string List = GenericCollectionsNamespace + "List";
        public const string IList = GenericCollectionsNamespace + "IList";
        public const string IReadOnlyCollection = GenericCollectionsNamespace + "IReadOnlyCollection";
        public const string IReadOnlyList = GenericCollectionsNamespace + "IReadOnlyList";
        public const string HashSet = GenericCollectionsNamespace + "HashSet";
        public const string ISet = GenericCollectionsNamespace + "ISet";
        public const string IReadOnlySpan = "global::System.ReadOnlySpan";
        public const string DateTimeOffset = "global::System.DateTimeOffset";
        public const string OrdinalStringComparisson = "global::System.StringComparison.Ordinal";
        public const string Func = "global::System.Func";
        public const string Task = "global::System.Threading.Tasks.Task";
        public const string IOperationObservable = "global::System.IObservable";
        public const string CancellationToken = "global::System.Threading.CancellationToken";
        public const string NotSupportedException = "global::System.NotSupportedException";
        public const string ArgumentNullException = "global::System.ArgumentNullException";
        public const string ArgumentException = "global::System.ArgumentException";

        public const string IServiceCollection = DependencyInjectionNamepsace + "IServiceCollection";
        public const string ServiceCollection = DependencyInjectionNamepsace + "ServiceCollection";
        public const string GetRequiredService = DependencyInjectionNamepsace +
            "ServiceProviderServiceExtensions.GetRequiredService";
        public const string AddSingleton = DependencyInjectionNamepsace +
            "ServiceCollectionServiceExtensions.AddSingleton";
        public const string IHttpClientFactory = "global::System.Net.Http.IHttpClientFactory";
        public const string TryAddSingleton= DependencyInjectionExtensions + "TryAddSingleton";
        public const string GenericCollectionsNamespace = "global::System.Collections.Generic.";
        public const string StrawberryshakeNamespace = "global::StrawberryShake.";
        public const string Dictionary = "global::System.Collections.Generic.Dictionary";
        public const string KeyValuePair = "global::System.Collections.Generic.KeyValuePair";
        public const string DependencyInjectionNamepsace = "global::Microsoft.Extensions.DependencyInjection.";
        public const string DependencyInjectionExtensions =  DependencyInjectionNamepsace +"Extensions.ServiceCollectionDescriptorExtensions.";

        public const string StringSerializer = StrawberryshakeNamespace + "Serialization.StringSerializer";
        public const string BooleanSerializer = StrawberryshakeNamespace + "Serialization.BooleanSerializer";
        public const string ByteSerializer = StrawberryshakeNamespace + "Serialization.ByteSerializer";
        public const string ShortSerializer = StrawberryshakeNamespace + "Serialization.ShortSerializer";
        public const string IntSerializer = StrawberryshakeNamespace + "Serialization.IntSerializer";
        public const string LongSerializer = StrawberryshakeNamespace + "Serialization.LongSerializer";
        public const string FloatSerializer = StrawberryshakeNamespace + "Serialization.FloatSerializer";
        public const string DecimalSerializer = StrawberryshakeNamespace + "Serialization.DecimalSerializer";
        public const string UrlSerializer = StrawberryshakeNamespace + "Serialization.UrlSerializer";
        public const string UuidSerializer = StrawberryshakeNamespace + "Serialization.UuidSerializer";
        public const string IdSerializer = StrawberryshakeNamespace + "Serialization.IdSerializer";
        public const string DateTimeSerializer = StrawberryshakeNamespace + "Serialization.DateTimeSerializer";
        public const string DateSerializer = StrawberryshakeNamespace + "Serialization.DateSerializer";
        public const string ByteArraySerializer = StrawberryshakeNamespace + "Serialization.ByteArraySerializer";
        public const string TimeSpanSerializer = StrawberryshakeNamespace + "Serialization.TimeSpanSerializer";
    }
}
