using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.Types.Relay.NodeFieldResolvers;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This type interceptor adds the fields `node` and the `nodes` to the query type.
/// </summary>
internal sealed class NodeFieldTypeInterceptor : TypeInterceptor
{
    private const int MaxStackallocTypeNameSize = 256;

    private ITypeCompletionContext? _queryContext;
    private ObjectTypeConfiguration? _queryTypeConfig;
    private TypeReference _nodeType = null!;
    private TypeReference _lookupRef = null!;
    private TypeReference _shareableRef = null!;
    private TypeReference _inaccessibleRef = null!;
    private bool _registeredTypes;
    private GlobalObjectIdentificationOptions _options = null!;
    private IReadOnlySchemaOptions _schemaOptions = null!;

    internal override uint Position => uint.MaxValue - 100;

    // node fields that are marked inaccessible are always marked shareable as well.
    private bool MarkNodeFieldsShareable
        => _schemaOptions.ApplyShareableToNodeFields
            || _schemaOptions.ApplyInaccessibleToNodeFields;

    public override bool IsEnabled(IDescriptorContext context)
    {
        var feature = context.Features.Get<NodeSchemaFeature>();
        return feature?.Options.RegisterNodeInterface ?? false;
    }

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _nodeType = context.TypeInspector.GetTypeRef(typeof(NodeType));
        _lookupRef = context.TypeInspector.GetTypeRef(typeof(Lookup));
        _shareableRef = context.TypeInspector.GetTypeRef(typeof(Shareable));
        _inaccessibleRef = context.TypeInspector.GetTypeRef(typeof(Inaccessible));
        _options = context.Features.GetRequired<NodeSchemaFeature>().Options;
        _schemaOptions = context.Options;
    }

    public override IEnumerable<TypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        if (!_registeredTypes)
        {
            yield return _nodeType;

            if (_options.MarkNodeFieldAsLookup)
            {
                yield return _lookupRef;
            }

            if (_options.MarkNodeFieldAsLookup || MarkNodeFieldsShareable)
            {
                yield return _shareableRef;
            }

            if (_schemaOptions.ApplyInaccessibleToNodeFields)
            {
                yield return _inaccessibleRef;
            }

            _registeredTypes = true;
        }
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryContext = completionContext;
            _queryTypeConfig = configuration;
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        if (_queryContext is not null && _queryTypeConfig is not null)
        {
            var typeInspector = _queryContext.TypeInspector;
            var serializer = _queryContext.DescriptorContext.NodeIdSerializerAccessor;

            // the nodes fields shall be chained in after the introspection fields,
            // so we first get the index of the last introspection field,
            // which is __typename
            var typeNameField = _queryTypeConfig.Fields.First(t =>
                t.Name.EqualsOrdinal(IntrospectionFieldNames.TypeName) && t.IsIntrospectionField);
            var index = _queryTypeConfig.Fields.IndexOf(typeNameField);
            var maxAllowedNodes = _options.MaxAllowedNodeBatchSize;
            var markNodeFieldShareable = MarkNodeFieldsShareable;
            var markNodeFieldInaccessible = _schemaOptions.ApplyInaccessibleToNodeFields;

            CreateNodeField(
                typeInspector,
                serializer,
                _queryTypeConfig.Fields,
                index + 1,
                _options.MarkNodeFieldAsLookup,
                markNodeFieldShareable,
                markNodeFieldInaccessible);

            if (_options.AddNodesField)
            {
                CreateNodesField(
                    typeInspector,
                    serializer,
                    _queryTypeConfig.Fields,
                    index + 2,
                    maxAllowedNodes,
                    markNodeFieldShareable,
                    markNodeFieldInaccessible);
            }
        }
    }

    private static void CreateNodeField(
        ITypeInspector typeInspector,
        INodeIdSerializerAccessor serializerAccessor,
        IList<ObjectFieldConfiguration> fields,
        int index,
        bool markNodeFieldAsLookup,
        bool markNodeFieldShareable,
        bool markNodeFieldInaccessible)
    {
        var node = typeInspector.GetTypeRef(typeof(NodeType));
        var id = typeInspector.GetTypeRef(typeof(NonNullType<IdType>));

        var field = new ObjectFieldConfiguration(
            Node,
            Relay_NodeField_Description,
            node)
        {
            Arguments =
            {
                new ArgumentConfiguration(Id, Relay_NodeField_Id_Description, id)
            },
            BatchResolver = contexts => ResolveNodeBatchAsync(contexts, serializerAccessor),
            BatchPartitionKeyResolver = NodePartitioner(serializerAccessor),
            Flags = CoreFieldFlags.ParallelExecutable | CoreFieldFlags.GlobalIdNodeField | CoreFieldFlags.BatchResolver
        };

        if (markNodeFieldAsLookup)
        {
            field.AddDirective(Lookup.Instance, typeInspector);
        }

        if (markNodeFieldShareable || markNodeFieldAsLookup)
        {
            field.AddDirective(Shareable.Instance, typeInspector);
        }

        if (markNodeFieldInaccessible)
        {
            field.AddDirective(Inaccessible.Instance, typeInspector);
        }

        // In the projection interceptor we want to change the context data on this field
        // after the field is completed. We need at least 1 element on the context data to avoid
        // it being replaced with ReadOnlyFeatureCollection.Default
        field.TouchFeatures();

        fields.Insert(index, field);
    }

    private static void CreateNodesField(
        ITypeInspector typeInspector,
        INodeIdSerializerAccessor serializerAccessor,
        IList<ObjectFieldConfiguration> fields,
        int index,
        int maxAllowedNodes,
        bool markNodeFieldShareable,
        bool markNodeFieldInaccessible)
    {
        var nodes = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NodeType>>));
        var ids = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NonNullType<IdType>>>));

        var field = new ObjectFieldConfiguration(
            Nodes,
            Relay_NodesField_Description,
            nodes)
        {
            Arguments =
            {
                new ArgumentConfiguration(Ids, Relay_NodesField_Ids_Description, ids)
            },
            BatchResolver = contexts =>
                ResolveNodesBatchAsync(contexts, serializerAccessor, maxAllowedNodes),
            Flags = CoreFieldFlags.ParallelExecutable | CoreFieldFlags.GlobalIdNodesField | CoreFieldFlags.BatchResolver
        };

        if (markNodeFieldShareable)
        {
            field.AddDirective(Shareable.Instance, typeInspector);
        }

        if (markNodeFieldInaccessible)
        {
            field.AddDirective(Inaccessible.Instance, typeInspector);
        }

        // In the projection interceptor we want to change the context data on this field
        // after the field is completed. We need at least 1 element on the context data to avoid
        // it being replaced with ReadOnlyFeatureCollection.Default.
        field.TouchFeatures();

        fields.Insert(index, field);
    }

    private static BatchPartitionKeyResolver NodePartitioner(
        INodeIdSerializerAccessor serializerAccessor)
    {
        INodeIdSerializer? serializer = null;
        return context =>
        {
            serializer ??= serializerAccessor.Serializer;

            var deserializedId = ResolveOrParseNodeId(context, serializer);
            var typeName = deserializedId.TypeName;

            var innerKey = 0UL;
            if (context.Schema.Types.TryGetType<ObjectType>(typeName, out var type)
                && type.Features.Get<NodeTypeFeature>() is { NodeResolver.BatchPartitionKey: { } inner })
            {
                innerKey = inner(context);
            }

            return ComposePartitionKey(innerKey, typeName);
        };
    }

    private static NodeId ResolveOrParseNodeId(
        IMiddlewareContext context,
        INodeIdSerializer serializer)
    {
        if (context.LocalContextData.TryGetValue(IdValue, out var cached) && cached is NodeId cachedId)
        {
            return cachedId;
        }

        // A malformed id or a non-string id literal throws here, which the engine isolates to
        // this context so it cannot poison its sibling contexts in the same batch. The argument
        // read stays inside this path so an incompatible literal surfaces the same isolated error.
        var literal = context.ArgumentLiteral<StringValueNode>(Id);
        var deserializedId = serializer.Parse(literal.Value, Unsafe.As<Schema>(context.Schema));
        context.SetLocalState(IdValue, deserializedId);
        return deserializedId;
    }

    private static ulong ComposePartitionKey(ulong innerKey, string typeName)
    {
        var typeBytes = Encoding.UTF8.GetByteCount(typeName);
        var length = sizeof(ulong) + typeBytes;
        byte[]? rented = null;
        Span<byte> buffer = length <= MaxStackallocTypeNameSize
            ? stackalloc byte[length]
            : rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, innerKey);
            Encoding.UTF8.GetBytes(typeName, buffer[sizeof(ulong)..]);

            return XxHash64.HashToUInt64(buffer[..length]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
