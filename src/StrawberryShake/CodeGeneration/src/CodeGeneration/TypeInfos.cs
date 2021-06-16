using System.Collections.Generic;
using StrawberryShake.CodeGeneration.Analyzers.Types;
using static StrawberryShake.CodeGeneration.TypeNames;

namespace StrawberryShake.CodeGeneration
{
    public class TypeInfos
    {
        private readonly Dictionary<string, RuntimeTypeInfo> _infos = new()
        {
            { IEntityStore, new RuntimeTypeInfo(IEntityStore) },
            { EntityStore, new RuntimeTypeInfo(EntityStore) },
            { IOperationStore, new RuntimeTypeInfo(IOperationStore) },
            { OperationStore, new RuntimeTypeInfo(OperationStore) },
            { IOperationExecutor, new RuntimeTypeInfo(IOperationExecutor) },
            { OperationExecutor, new RuntimeTypeInfo(OperationExecutor) },
            { IOperationResult, new RuntimeTypeInfo(IOperationResult) },
            { OperationResult, new RuntimeTypeInfo(OperationResult) },
            { IOperationResultDataFactory, new RuntimeTypeInfo(IOperationResultDataFactory) },
            { IOperationResultDataInfo, new RuntimeTypeInfo(IOperationResultDataInfo) },
            { IOperationResultBuilder, new RuntimeTypeInfo(IOperationResultBuilder) },
            { ISerializerResolver, new RuntimeTypeInfo(ISerializerResolver) },
            { SerializerResolver, new RuntimeTypeInfo(SerializerResolver) },
            { ISerializer, new RuntimeTypeInfo(ISerializer) },
            { ILeafValueParser, new RuntimeTypeInfo(ILeafValueParser) },
            { IInputValueFormatter, new RuntimeTypeInfo(IInputValueFormatter) },
            { IInputObjectFormatter, new RuntimeTypeInfo(IInputObjectFormatter) },
            { IConnection, new RuntimeTypeInfo(IConnection) },
            { IEntityMapper, new RuntimeTypeInfo(IEntityMapper) },
            { IDocument, new RuntimeTypeInfo(IDocument) },
            { GraphQLClientException, new RuntimeTypeInfo(GraphQLClientException) },
            { OperationKind, new RuntimeTypeInfo(OperationKind) },
            { EntityId, new RuntimeTypeInfo(EntityId) },
            { IEntityUpdateSession, new RuntimeTypeInfo(IEntityUpdateSession) },
            { IEntityUpdateSession_Version, new RuntimeTypeInfo(IEntityUpdateSession_Version) },
            { Execute, new RuntimeTypeInfo(Execute) },
            { Watch, new RuntimeTypeInfo(Watch) },
            { Response, new RuntimeTypeInfo(Response) },
            { OperationRequest, new RuntimeTypeInfo(OperationRequest) },
            { ExecutionStrategy, new RuntimeTypeInfo(ExecutionStrategy) },
            { HttpConnection, new RuntimeTypeInfo(HttpConnection) },
            { WebSocketConnection, new RuntimeTypeInfo(WebSocketConnection) },
            { ISessionPool, new RuntimeTypeInfo(ISessionPool) },
            { GraphQLWebSocketProtocolFactory, new RuntimeTypeInfo(GraphQLWebSocketProtocolFactory) },
            { JsonElement, new RuntimeTypeInfo(JsonElement, true) },
            { JsonDocument, new RuntimeTypeInfo(JsonDocument) },
            { String, new RuntimeTypeInfo(String) },
            { Byte, new RuntimeTypeInfo(Byte, true) },
            { ByteArray, new RuntimeTypeInfo(ByteArray, true) },
            { Int16, new RuntimeTypeInfo(Int16, true) },
            { Int32, new RuntimeTypeInfo(Int32, true) },
            { Int64, new RuntimeTypeInfo(Int64, true) },
            { UInt16, new RuntimeTypeInfo(UInt16, true) },
            { UInt32, new RuntimeTypeInfo(UInt32, true) },
            { UInt64, new RuntimeTypeInfo(UInt64, true) },
            { Single, new RuntimeTypeInfo(Single, true) },
            { Double, new RuntimeTypeInfo(Double, true) },
            { Decimal, new RuntimeTypeInfo(Decimal, true) },
            { Uri, new RuntimeTypeInfo(Uri) },
            { Boolean, new RuntimeTypeInfo(Boolean, true) },
            { Object, new RuntimeTypeInfo(Object) },
            { Guid, new RuntimeTypeInfo(Guid, true) },
            { DateTime, new RuntimeTypeInfo(DateTime, true) },
            { TimeSpan, new RuntimeTypeInfo(TimeSpan, true) },
            { EncodingUtf8, new RuntimeTypeInfo(EncodingUtf8) },
            { List, new RuntimeTypeInfo(List) },
            { IEnumerable, new RuntimeTypeInfo(IEnumerable) },
            { IList, new RuntimeTypeInfo(IList) },
            { IReadOnlyCollection, new RuntimeTypeInfo(IReadOnlyCollection) },
            { IReadOnlyList, new RuntimeTypeInfo(IReadOnlyList) },
            { HashSet, new RuntimeTypeInfo(HashSet) },
            { ISet, new RuntimeTypeInfo(ISet) },
            { IReadOnlySpan, new RuntimeTypeInfo(IReadOnlySpan, true) },
            { DateTimeOffset, new RuntimeTypeInfo(DateTimeOffset, true) },
            { Func, new RuntimeTypeInfo(Func) },
            { Task, new RuntimeTypeInfo(Task) },
            { IOperationObservable, new RuntimeTypeInfo(IOperationObservable) },
            { CancellationToken, new RuntimeTypeInfo(CancellationToken, true) },
            { NotSupportedException, new RuntimeTypeInfo(NotSupportedException) },
            { ArgumentNullException, new RuntimeTypeInfo(ArgumentNullException) },
            { ArgumentException, new RuntimeTypeInfo(ArgumentException) },
            { IServiceCollection, new RuntimeTypeInfo(IServiceCollection) },
            { IServiceProvider, new RuntimeTypeInfo(IServiceProvider) },
            { ServiceCollection, new RuntimeTypeInfo(ServiceCollection) },
            { BuildServiceProvider, new RuntimeTypeInfo(BuildServiceProvider) },
            { IHttpClientFactory, new RuntimeTypeInfo(IHttpClientFactory) },
            { Dictionary, new RuntimeTypeInfo(Dictionary) },
            { TypeNames.KeyValuePair, new RuntimeTypeInfo(TypeNames.KeyValuePair, true) },
            { StringSerializer, new RuntimeTypeInfo(StringSerializer) },
            { BooleanSerializer, new RuntimeTypeInfo(BooleanSerializer) },
            { ByteSerializer, new RuntimeTypeInfo(ByteSerializer) },
            { ShortSerializer, new RuntimeTypeInfo(ShortSerializer) },
            { IntSerializer, new RuntimeTypeInfo(IntSerializer) },
            { LongSerializer, new RuntimeTypeInfo(LongSerializer) },
            { FloatSerializer, new RuntimeTypeInfo(FloatSerializer) },
            { DecimalSerializer, new RuntimeTypeInfo(DecimalSerializer) },
            { UrlSerializer, new RuntimeTypeInfo(UrlSerializer) },
            { UUIDSerializer, new RuntimeTypeInfo(UUIDSerializer) },
            { IdSerializer, new RuntimeTypeInfo(IdSerializer) },
            { DateTimeSerializer, new RuntimeTypeInfo(DateTimeSerializer) },
            { DateSerializer, new RuntimeTypeInfo(DateSerializer) },
            { ByteArraySerializer, new RuntimeTypeInfo(ByteArraySerializer) },
            { TimeSpanSerializer, new RuntimeTypeInfo(TimeSpanSerializer) }
        };

        public RuntimeTypeInfo GetOrCreate(string fullTypeName, bool valueType = false) =>
            _infos.TryGetValue(fullTypeName, out RuntimeTypeInfo? typeInfo)
                ? typeInfo
                : new(fullTypeName, valueType);

        public RuntimeTypeInfo TryCreate(RuntimeTypeDirective runtimeType) =>
            _infos.TryGetValue(runtimeType.Name, out RuntimeTypeInfo? typeInfo)
                ? typeInfo
                : new(runtimeType.Name, runtimeType.ValueType ?? false);
    }
}
