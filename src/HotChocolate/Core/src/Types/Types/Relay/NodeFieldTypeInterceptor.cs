using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Relay.NodeConstants;
using static HotChocolate.Types.Relay.NodeFieldResolvers;

namespace HotChocolate.Types.Relay;

/// <summary>
/// This type interceptor adds the fields `node` and the `nodes` to the query type.
/// </summary>
internal sealed class NodeFieldTypeInterceptor : TypeInterceptor
{
    private ITypeCompletionContext? _queryContext;
    private ObjectTypeConfiguration? _queryTypeConfig;
    private TypeReference _nodeType = null!;
    private TypeReference _lookupRef = null!;
    private bool _registeredTypes;
    private GlobalObjectIdentificationOptions _options = null!;

    internal override uint Position => uint.MaxValue - 100;

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
        _options = context.Features.GetRequired<NodeSchemaFeature>().Options;
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

            CreateNodeField(
                typeInspector,
                serializer,
                _queryTypeConfig.Fields,
                index + 1,
                _options.MarkNodeFieldAsLookup);

            if (_options.AddNodesField)
            {
                CreateNodesField(
                    typeInspector,
                    serializer,
                    _queryTypeConfig.Fields,
                    index + 2,
                    maxAllowedNodes);
            }
        }
    }

    private static void CreateNodeField(
        ITypeInspector typeInspector,
        INodeIdSerializerAccessor serializerAccessor,
        IList<ObjectFieldConfiguration> fields,
        int index,
        bool markNodeFieldAsLookup)
    {
        var node = typeInspector.GetTypeRef(typeof(NodeType));
        var id = typeInspector.GetTypeRef(typeof(NonNullType<IdType>));

        var field = new ObjectFieldConfiguration(
            Node,
            Relay_NodeField_Description,
            node)
        {
            Arguments = { new ArgumentConfiguration(Id, Relay_NodeField_Id_Description, id) },
            MiddlewareConfigurations =
            {
                new FieldMiddlewareConfiguration(_ =>
                {
                    INodeIdSerializer? serializer = null;
                    return async context =>
                    {
                        serializer ??= serializerAccessor.Serializer;
                        await ResolveSingleNodeAsync(context, serializer).ConfigureAwait(false);
                    };
                })
            },
            Flags = CoreFieldFlags.ParallelExecutable | CoreFieldFlags.GlobalIdNodeField
        };

        if (markNodeFieldAsLookup)
        {
            // TODO: This doesn't work
            field.AddDirective(Lookup.Instance, typeInspector);
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
        int maxAllowedNodes)
    {
        var nodes = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NodeType>>));
        var ids = typeInspector.GetTypeRef(typeof(NonNullType<ListType<NonNullType<IdType>>>));

        var field = new ObjectFieldConfiguration(
            Nodes,
            Relay_NodesField_Description,
            nodes)
        {
            Arguments = { new ArgumentConfiguration(Ids, Relay_NodesField_Ids_Description, ids) },
            MiddlewareConfigurations =
            {
                new FieldMiddlewareConfiguration(_ =>
                {
                    INodeIdSerializer? serializer = null;
                    return async context =>
                    {
                        serializer ??= serializerAccessor.Serializer;
                        await ResolveManyNodeAsync(context, serializer, maxAllowedNodes).ConfigureAwait(false);
                    };
                })
            },
            Flags = CoreFieldFlags.ParallelExecutable | CoreFieldFlags.GlobalIdNodesField
        };

        // In the projection interceptor we want to change the context data on this field
        // after the field is completed. We need at least 1 element on the context data to avoid
        // it being replaced with ReadOnlyFeatureCollection.Default.
        field.TouchFeatures();

        fields.Insert(index, field);
    }
}
