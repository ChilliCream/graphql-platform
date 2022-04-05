using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Pagination;

internal class CollectionSegmentType
    : ObjectType
    , IPageType
{
    internal CollectionSegmentType(
        NameString? collectionSegmentName,
        ITypeReference nodeType,
        IDescriptorContext descriptorContext,
        bool withTotalCount)
    {
        if (nodeType is null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        var objectTypeDescriptor = ObjectTypeDescriptor.New<CollectionSegment>(descriptorContext);

        SyntaxTypeReference connectionSegmentType =
            TypeReference.Parse(
                $"[{nodeType}!]",
                TypeContext.Output,
                factory: _ => new EdgeType(collectionSegmentName, nodeType));

        Definition = CreateTypeDefinition(withTotalCount, connectionSegmentType);

        if (collectionSegmentName is not null)
        {
            Definition.Name = collectionSegmentName.Value + "CollectionSegment";
        }
        else
        {
            Definition.Configurations.Add(
                new CompleteConfiguration(
                    (c, d) =>
                    {
                        IType type = c.GetType<IType>(nodeType);
                        var definition = (ObjectTypeDefinition)d;

                        ObjectFieldDefinition nodes = definition.Fields.First(/* ??? */);
                        nodes.Type = TypeReference.Parse(
                            $"[{c.GetType<IType>(nodeType).Print()}]",
                            TypeContext.Output);
                        definition.Name = type.NamedType().Name + "CollectionSegment";
                    },
                    Definition,
                    ApplyConfigurationOn.Naming,
                    nodeType,
                    TypeDependencyKind.Named));
        }

        Definition.Dependencies.Add(new(nodeType));

    }

    /// <summary>
    /// Initializes <see cref="CollectionSegmentType{T}" />.
    /// </summary>
    //public CollectionSegmentType()
    //{
    //}

    /// <summary>
    /// Initializes <see cref="CollectionSegmentType{T}" />.
    /// </summary>
    /// <param name="configure">
    /// A delegate adding more configuration to the type.
    /// </param>
    //internal CollectionSegmentType(
    //    Action<IObjectTypeDescriptor<CollectionSegment>> configure)
    //    : base(descriptor =>
    //    {
    //        ApplyConfig(descriptor);
    //        configure(descriptor);
    //    })
    //{
    //}

    /// <summary>
    /// Gets the item type of this collection segment.
    /// </summary>
    public IOutputType ItemType { get; private set; } = default!;

    //protected override void Configure(IObjectTypeDescriptor<CollectionSegment> descriptor) =>
    //    ApplyConfig(descriptor);

    //protected static void ApplyConfig(IObjectTypeDescriptor<CollectionSegment> descriptor)
    //{
    //    //descriptor
    //    //    .Name(dependency => $"{dependency.Name}CollectionSegment")
    //    //    .DependsOn<T>()
    //    //    .BindFieldsExplicitly();

    //    descriptor
    //        .Field(i => i.Items)
    //        .Name("items")
    //        .Type<ListType<T>>();

    //    descriptor
    //        .Field(t => t.Info)
    //        .Name("pageInfo")
    //        .Description("Information to aid in pagination.")
    //        .Type<NonNullType<CollectionSegmentInfoType>>();
    //}

    protected override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        DefinitionBase definition,
        IDictionary<string, object?> contextData)
    {
        context.Dependencies.Add(new(
            context.TypeInspector.GetOutputTypeRef(typeof(PageInfoType))));

        base.OnBeforeRegisterDependencies(context, definition, contextData);
    }

    //protected override void OnCompleteType(
    //    ITypeCompletionContext context,
    //    ObjectTypeDefinition definition)
    //{
    //    base.OnCompleteType(context, definition);

    //    ItemType = context.GetType<IOutputType>(
    //        context.TypeInspector.GetTypeRef(typeof(T)));
    //}

    private static ObjectTypeDefinition CreateTypeDefinition(
        bool withTotalCount,
        ITypeReference? edgesType = null)
    {
        var definition = new ObjectTypeDefinition(
            default,
            CollectionSegmentType_PageInfo_Description,
            typeof(Connection));

        definition.Fields.Add(new(
            Names.PageInfo,
            ConnectionType_PageInfo_Description,
            TypeReference.Parse("PageInfo!"),
            pureResolver: GetPagingInfo));

        definition.Fields.Add(new(
                Names.Nodes,
                ConnectionType_Nodes_Description,
                pureResolver: GetNodes)
            { CustomSettings = { ContextDataKeys.Nodes } });

        if (withTotalCount)
        {
            definition.Fields.Add(new(
                Names.TotalCount,
                type: TypeReference.Parse($"{ScalarNames.Int}!"),
                resolver: GetTotalCountAsync));
        }

        return definition;
    }

    private static IPageInfo GetPagingInfo(IPureResolverContext context)
        => context.Parent<Connection>().Info;

    private static IEnumerable<object?> GetNodes(IPureResolverContext context)
        => context.Parent<Connection>().Edges.Select(t => t.Node);

    private static async ValueTask<object?> GetTotalCountAsync(IResolverContext context)
        => await context.Parent<Connection>().GetTotalCountAsync(context.RequestAborted);

    internal static class Names
    {
        public const string PageInfo = "pageInfo";
        public const string Edges = "edges";
        public const string Nodes = "nodes";
        public const string TotalCount = "totalCount";
    }
    private static class ContextDataKeys
    {
        public const string EdgeType = "HotChocolate_Types_Edge";
        public const string Edges = "HotChocolate.Types.Connection.Edges";
        public const string Nodes = "HotChocolate.Types.Connection.Nodes";
    }
}
