using System;

namespace StrawberryShake.CodeGeneration
{
    public static class TypeNames
    {
        public const string IEntityStore = StrawberryshakeNamespace + "IEntityStore";
        public const string IOperationExecutor = StrawberryshakeNamespace + "IOperationExecutor";
        public const string IOperationResult = StrawberryshakeNamespace + "IOperationResult";
        public const string OperationResult = StrawberryshakeNamespace + "OperationResult";
        public const string IOperationResultDataFactory = StrawberryshakeNamespace + "IOperationResultDataFactory";
        public const string IOperationResultDataInfo = StrawberryshakeNamespace + "IOperationResultDataInfo";
        public const string IOperationResultBuilder = StrawberryshakeNamespace + "IOperationResultBuilder";
        public const string ISerializerResolver = StrawberryshakeNamespace + "Serialization.ISerializerResolver";
        public const string ILeafValueParser = StrawberryshakeNamespace + "Serialization.ILeafValueParser";
        public const string IEntityMapper = StrawberryshakeNamespace + "IEntityMapper";
        public const string IDocument = StrawberryshakeNamespace + "IDocument";
        public const string IGraphQLClientException = StrawberryshakeNamespace + "GraphQLClientException";

        public const string OperationKind = StrawberryshakeNamespace + "OperationKind";
        public const string EntityId = StrawberryshakeNamespace + "EntityId";
        public const string IEntityUpdateSession = StrawberryshakeNamespace + "IEntityUpdateSession";
        public const string IEntityUpdateSession_Version = "Version";

        public const string Execute = "Execute";
        public const string Watch = "Watch";
        public const string Response = StrawberryshakeNamespace + "Response";
        public const string OperationRequest = StrawberryshakeNamespace + "OperationRequest";
        public const string ExecutionStrategy = StrawberryshakeNamespace + "ExecutionStrategy";
        public const string GetPropertyOrNull = StrawberryshakeNamespace + "Transport.Http.JsonElementExtensions.GetPropertyOrNull";

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
        public const string Guid = "global::System.Guid";
        public const string DateTime = "global::System.DateTime";
        public const string EncodingUtf8 = "global::System.Text.Encoding.UTF8";
        public const string List = GenericCollectionsNamespace + "List";
        public const string IList = GenericCollectionsNamespace + "IList";
        public const string IReadOnlyCollection = GenericCollectionsNamespace + "IReadOnlyCollection";
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

        public const string GenericCollectionsNamespace = "global::System.Collections.Generic.";
        public const string StrawberryshakeNamespace = "global::StrawberryShake.";
        public const string Dictionary = "global::System.Collections.Generic.Dictionary";
    }
}
