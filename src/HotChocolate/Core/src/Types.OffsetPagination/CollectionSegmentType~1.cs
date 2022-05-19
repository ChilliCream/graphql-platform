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
        bool withTotalCount)
    {
        if (nodeType is null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        Definition = CreateTypeDefinition(withTotalCount);

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
                        definition.Name = type.NamedType().Name + "CollectionSegment";
                    },
                    Definition,
                    ApplyConfigurationOn.Naming,
                    nodeType,
                    TypeDependencyKind.Named));
        }

        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, d) =>
                {
                    ItemType = c.GetType<IOutputType>(nodeType);

                    var definition = (ObjectTypeDefinition)d;
                    ObjectFieldDefinition nodes = definition.Fields.First(IsItemsField);
                    nodes.Type = TypeReference.Parse($"[{ItemType.Print()}]", TypeContext.Output);
                },
                Definition,
                ApplyConfigurationOn.Naming,
                nodeType,
                TypeDependencyKind.Named));

        Definition.Dependencies.Add(new(nodeType));
    }

    /// <summary>
    /// Gets the item type of this collection segment.
    /// </summary>
    public IOutputType ItemType { get; private set; } = default!;

    protected override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        DefinitionBase definition,
        IDictionary<string, object?> contextData)
    {
        context.Dependencies.Add(new(
            context.TypeInspector.GetOutputTypeRef(typeof(CollectionSegmentInfoType))));

        base.OnBeforeRegisterDependencies(context, definition, contextData);
    }

    private static ObjectTypeDefinition CreateTypeDefinition(bool withTotalCount)
    {
        var definition = new ObjectTypeDefinition(
            default,
            CollectionSegmentType_Description,
            typeof(CollectionSegment));

        definition.Fields.Add(new(
            Names.PageInfo,
            CollectionSegmentType_PageInfo_Description,
            TypeReference.Parse("CollectionSegmentInfo!"),
            pureResolver: GetPagingInfo));

        definition.Fields.Add(new(
            Names.Items,
            CollectionSegmentType_Items_Description,
            pureResolver: GetItems) {CustomSettings = {ContextDataKeys.Items}});

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
        => context.Parent<CollectionSegment>().Info;

    private static IEnumerable<object?> GetItems(IPureResolverContext context)
        => context.Parent<CollectionSegment>().Items;

    private static async ValueTask<object?> GetTotalCountAsync(IResolverContext context)
        => await context.Parent<CollectionSegment>().GetTotalCountAsync(context.RequestAborted);

    private static bool IsItemsField(ObjectFieldDefinition field)
        => field.CustomSettings.Count > 0 &&
            field.CustomSettings[0].Equals(ContextDataKeys.Items);

    internal static class Names
    {
        public const string PageInfo = "pageInfo";
        public const string Items = "items";
        public const string TotalCount = "totalCount";
    }

    private static class ContextDataKeys
    {
        public const string Items = "HotChocolate.Types.CollectionSegment.Items";
    }
}
