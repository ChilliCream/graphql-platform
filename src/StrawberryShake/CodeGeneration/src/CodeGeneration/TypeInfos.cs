using System.Diagnostics;
using StrawberryShake.CodeGeneration.Analyzers.Types;

namespace StrawberryShake.CodeGeneration;

public sealed class TypeInfos
{
    private readonly Dictionary<string, RuntimeTypeInfo> _infos = new()
    {
        {
            TypeNames.IEntityStore,
            new RuntimeTypeInfo(TypeNames.IEntityStore)
        },
        {
            TypeNames.EntityStore,
            new RuntimeTypeInfo(TypeNames.EntityStore)
        },
        {
            TypeNames.IOperationStore,
            new RuntimeTypeInfo(TypeNames.IOperationStore)
        },
        {
            TypeNames.OperationStore,
            new RuntimeTypeInfo(TypeNames.OperationStore)
        },
        {
            TypeNames.IOperationExecutor,
            new RuntimeTypeInfo(TypeNames.IOperationExecutor)
        },
        {
            TypeNames.OperationExecutor,
            new RuntimeTypeInfo(TypeNames.OperationExecutor)
        },
        {
            TypeNames.IOperationResult,
            new RuntimeTypeInfo(TypeNames.IOperationResult)
        },
        {
            TypeNames.OperationResult,
            new RuntimeTypeInfo(TypeNames.OperationResult)
        },
        {
            TypeNames.IOperationResultDataFactory,
            new RuntimeTypeInfo(TypeNames.IOperationResultDataFactory)
        },
        {
            TypeNames.IOperationResultDataInfo,
            new RuntimeTypeInfo(TypeNames.IOperationResultDataInfo)
        },
        {
            TypeNames.IOperationResultBuilder,
            new RuntimeTypeInfo(TypeNames.IOperationResultBuilder)
        },
        {
            TypeNames.ISerializerResolver,
            new RuntimeTypeInfo(TypeNames.ISerializerResolver)
        },
        {
            TypeNames.SerializerResolver,
            new RuntimeTypeInfo(TypeNames.SerializerResolver)
        },
        {
            TypeNames.ISerializer,
            new RuntimeTypeInfo(TypeNames.ISerializer)
        },
        {
            TypeNames.ILeafValueParser,
            new RuntimeTypeInfo(TypeNames.ILeafValueParser)
        },
        {
            TypeNames.IInputValueFormatter,
            new RuntimeTypeInfo(TypeNames.IInputValueFormatter)
        },
        {
            TypeNames.IInputObjectFormatter,
            new RuntimeTypeInfo(TypeNames.IInputObjectFormatter)
        },
        {
            TypeNames.IConnection,
            new RuntimeTypeInfo(TypeNames.IConnection)
        },
        {
            TypeNames.IEntityMapper,
            new RuntimeTypeInfo(TypeNames.IEntityMapper)
        },
        {
            TypeNames.IDocument,
            new RuntimeTypeInfo(TypeNames.IDocument)
        },
        {
            TypeNames.GraphQLClientException,
            new RuntimeTypeInfo(TypeNames.GraphQLClientException)
        },
        {
            TypeNames.OperationKind,
            new RuntimeTypeInfo(TypeNames.OperationKind)
        },
        {
            TypeNames.EntityId,
            new RuntimeTypeInfo(TypeNames.EntityId)
        },
        {
            TypeNames.IEntityUpdateSession,
            new RuntimeTypeInfo(TypeNames.IEntityUpdateSession)
        },
        {
            TypeNames.IEntityUpdateSession_Version,
            new RuntimeTypeInfo(TypeNames.IEntityUpdateSession_Version)
        },
        {
            TypeNames.Execute,
            new RuntimeTypeInfo(TypeNames.Execute)
        },
        {
            TypeNames.Watch,
            new RuntimeTypeInfo(TypeNames.Watch)
        },
        {
            TypeNames.Response,
            new RuntimeTypeInfo(TypeNames.Response)
        },
        {
            TypeNames.OperationRequest,
            new RuntimeTypeInfo(TypeNames.OperationRequest)
        },
        {
            TypeNames.ExecutionStrategy,
            new RuntimeTypeInfo(TypeNames.ExecutionStrategy)
        },
        {
            TypeNames.HttpConnection,
            new RuntimeTypeInfo(TypeNames.HttpConnection)
        },
        {
            TypeNames.WebSocketConnection,
            new RuntimeTypeInfo(TypeNames.WebSocketConnection)
        },
        {
            TypeNames.ISessionPool,
            new RuntimeTypeInfo(TypeNames.ISessionPool)
        },
        {
            TypeNames.GraphQLWebSocketProtocolFactory,
            new RuntimeTypeInfo(TypeNames.GraphQLWebSocketProtocolFactory)
        },
        {
            TypeNames.JsonElement,
            new RuntimeTypeInfo(TypeNames.JsonElement, true)
        },
        {
            TypeNames.JsonDocument,
            new RuntimeTypeInfo(TypeNames.JsonDocument)
        },
        {
            TypeNames.Upload,
            new RuntimeTypeInfo(TypeNames.Upload, true)
        },
        {
            TypeNames.String,
            new RuntimeTypeInfo(TypeNames.String)
        },
        {
            TypeNames.Byte,
            new RuntimeTypeInfo(TypeNames.Byte, true)
        },
        {
            TypeNames.ByteArray,
            new RuntimeTypeInfo(TypeNames.ByteArray, true)
        },
        {
            TypeNames.Int16,
            new RuntimeTypeInfo(TypeNames.Int16, true)
        },
        {
            TypeNames.Int32,
            new RuntimeTypeInfo(TypeNames.Int32, true)
        },
        {
            TypeNames.Int64,
            new RuntimeTypeInfo(TypeNames.Int64, true)
        },
        {
            TypeNames.UInt16,
            new RuntimeTypeInfo(TypeNames.UInt16, true)
        },
        {
            TypeNames.UInt32,
            new RuntimeTypeInfo(TypeNames.UInt32, true)
        },
        {
            TypeNames.UInt64,
            new RuntimeTypeInfo(TypeNames.UInt64, true)
        },
        {
            TypeNames.Single,
            new RuntimeTypeInfo(TypeNames.Single, true)
        },
        {
            TypeNames.Double,
            new RuntimeTypeInfo(TypeNames.Double, true)
        },
        {
            TypeNames.Decimal,
            new RuntimeTypeInfo(TypeNames.Decimal, true)
        },
        {
            TypeNames.Uri,
            new RuntimeTypeInfo(TypeNames.Uri)
        },
        {
            TypeNames.Boolean,
            new RuntimeTypeInfo(TypeNames.Boolean, true)
        },
        {
            TypeNames.Object,
            new RuntimeTypeInfo(TypeNames.Object)
        },
        {
            TypeNames.Guid,
            new RuntimeTypeInfo(TypeNames.Guid, true)
        },
        {
            TypeNames.DateTime,
            new RuntimeTypeInfo(TypeNames.DateTime, true)
        },
        {
            TypeNames.TimeSpan,
            new RuntimeTypeInfo(TypeNames.TimeSpan, true)
        },
        {
            TypeNames.EncodingUtf8,
            new RuntimeTypeInfo(TypeNames.EncodingUtf8)
        },
        {
            TypeNames.List,
            new RuntimeTypeInfo(TypeNames.List)
        },
        {
            TypeNames.IEnumerable,
            new RuntimeTypeInfo(TypeNames.IEnumerable)
        },
        {
            TypeNames.IList,
            new RuntimeTypeInfo(TypeNames.IList)
        },
        {
            TypeNames.IReadOnlyCollection,
            new RuntimeTypeInfo(TypeNames.IReadOnlyCollection)
        },
        {
            TypeNames.IReadOnlyList,
            new RuntimeTypeInfo(TypeNames.IReadOnlyList)
        },
        {
            TypeNames.HashSet,
            new RuntimeTypeInfo(TypeNames.HashSet)
        },
        {
            TypeNames.ISet,
            new RuntimeTypeInfo(TypeNames.ISet)
        },
        {
            TypeNames.IReadOnlySpan,
            new RuntimeTypeInfo(TypeNames.IReadOnlySpan, true)
        },
        {
            TypeNames.DateTimeOffset,
            new RuntimeTypeInfo(TypeNames.DateTimeOffset, true)
        },
        {
            TypeNames.Func,
            new RuntimeTypeInfo(TypeNames.Func)
        },
        {
            TypeNames.Task,
            new RuntimeTypeInfo(TypeNames.Task)
        },
        {
            TypeNames.IOperationObservable,
            new RuntimeTypeInfo(TypeNames.IOperationObservable)
        },
        {
            TypeNames.CancellationToken,
            new RuntimeTypeInfo(TypeNames.CancellationToken, true)
        },
        {
            TypeNames.NotSupportedException,
            new RuntimeTypeInfo(TypeNames.NotSupportedException)
        },
        {
            TypeNames.ArgumentNullException,
            new RuntimeTypeInfo(TypeNames.ArgumentNullException)
        },
        {
            TypeNames.ArgumentException,
            new RuntimeTypeInfo(TypeNames.ArgumentException)
        },
        {
            TypeNames.IServiceCollection,
            new RuntimeTypeInfo(TypeNames.IServiceCollection)
        },
        {
            TypeNames.IServiceProvider,
            new RuntimeTypeInfo(TypeNames.IServiceProvider)
        },
        {
            TypeNames.ServiceCollection,
            new RuntimeTypeInfo(TypeNames.ServiceCollection)
        },
        {
            TypeNames.BuildServiceProvider,
            new RuntimeTypeInfo(TypeNames.BuildServiceProvider)
        },
        {
            TypeNames.IHttpClientFactory,
            new RuntimeTypeInfo(TypeNames.IHttpClientFactory)
        },
        {
            TypeNames.Dictionary,
            new RuntimeTypeInfo(TypeNames.Dictionary)
        },
        {
            TypeNames.KeyValuePair,
            new RuntimeTypeInfo(TypeNames.KeyValuePair, true)
        },
        {
            TypeNames.StringSerializer,
            new RuntimeTypeInfo(TypeNames.StringSerializer)
        },
        {
            TypeNames.BooleanSerializer,
            new RuntimeTypeInfo(TypeNames.BooleanSerializer)
        },
        {
            TypeNames.ByteSerializer,
            new RuntimeTypeInfo(TypeNames.ByteSerializer)
        },
        {
            TypeNames.ShortSerializer,
            new RuntimeTypeInfo(TypeNames.ShortSerializer)
        },
        {
            TypeNames.IntSerializer,
            new RuntimeTypeInfo(TypeNames.IntSerializer)
        },
        {
            TypeNames.LongSerializer,
            new RuntimeTypeInfo(TypeNames.LongSerializer)
        },
        {
            TypeNames.FloatSerializer,
            new RuntimeTypeInfo(TypeNames.FloatSerializer)
        },
        {
            TypeNames.DecimalSerializer,
            new RuntimeTypeInfo(TypeNames.DecimalSerializer)
        },
        {
            TypeNames.UrlSerializer,
            new RuntimeTypeInfo(TypeNames.UrlSerializer)
        },
        {
            TypeNames.UUIDSerializer,
            new RuntimeTypeInfo(TypeNames.UUIDSerializer)
        },
        {
            TypeNames.IdSerializer,
            new RuntimeTypeInfo(TypeNames.IdSerializer)
        },
        {
            TypeNames.DateTimeSerializer,
            new RuntimeTypeInfo(TypeNames.DateTimeSerializer)
        },
        {
            TypeNames.DateSerializer,
            new RuntimeTypeInfo(TypeNames.DateSerializer)
        },
        {
            TypeNames.ByteArraySerializer,
            new RuntimeTypeInfo(TypeNames.ByteArraySerializer)
        },
        {
            TypeNames.TimeSpanSerializer,
            new RuntimeTypeInfo(TypeNames.TimeSpanSerializer)
        },
    };

    public RuntimeTypeInfo GetOrAdd(string fullTypeName, bool valueType = false) =>
        GetOrAdd(fullTypeName, () => new(fullTypeName, valueType));

    public RuntimeTypeInfo GetOrAdd(RuntimeTypeDirective runtimeType) =>
        GetOrAdd(runtimeType.Name, () => new(runtimeType.Name, runtimeType.ValueType ?? false));

    private RuntimeTypeInfo GetOrAdd(string fullTypeName, Func<RuntimeTypeInfo> factory)
    {
        if (!fullTypeName.StartsWith("global::"))
        {
            fullTypeName = "global::" + fullTypeName;
        }

        if (!_infos.TryGetValue(fullTypeName, out var typeInfo))
        {
            typeInfo = factory();
            Debug.Assert(
                typeInfo.FullName == fullTypeName,
                $"Expected generated type '{typeInfo.FullName}' to equal '{fullTypeName}'.");
            _infos.Add(fullTypeName, typeInfo);
        }

        return typeInfo;
    }
}
