using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Pagination;

internal class CollectionSegmentType : ObjectType, IPageType
{
    internal CollectionSegmentType(
        string? collectionSegmentName,
        TypeReference nodeType,
        bool withTotalCount)
    {
        ArgumentNullException.ThrowIfNull(nodeType);

        Configuration = CreateConfiguration(withTotalCount);

        if (collectionSegmentName is not null)
        {
            Configuration.Name = collectionSegmentName + "CollectionSegment";
        }
        else
        {
            Configuration.Tasks.Add(
                new OnCompleteTypeSystemConfigurationTask(
                    (c, d) =>
                    {
                        var type = c.GetType<IType>(nodeType);
                        var definition = (ObjectTypeConfiguration)d;
                        definition.Name = type.NamedType().Name + "CollectionSegment";
                    },
                    Configuration,
                    ApplyConfigurationOn.BeforeNaming,
                    nodeType,
                    TypeDependencyFulfilled.Named));
        }

        Configuration.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask(
                (c, d) =>
                {
                    ItemType = c.GetType<IOutputType>(nodeType);

                    var definition = (ObjectTypeConfiguration)d;
                    var nodes = definition.Fields.First(IsItemsField);
                    nodes.Type = TypeReference.Parse($"[{ItemType.Print()}]", TypeContext.Output);
                },
                Configuration,
                ApplyConfigurationOn.BeforeNaming,
                nodeType,
                TypeDependencyFulfilled.Named));

        Configuration.Dependencies.Add(new(nodeType));
    }

    /// <summary>
    /// Gets the item type of this collection segment.
    /// </summary>
    public IOutputType ItemType { get; private set; } = null!;

    protected override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration configuration)
    {
        var typeRef = context.TypeInspector.GetOutputTypeRef(typeof(CollectionSegmentInfoType));
        context.Dependencies.Add(new(typeRef));
        base.OnBeforeRegisterDependencies(context, configuration);
    }

    private static ObjectTypeConfiguration CreateConfiguration(bool withTotalCount)
    {
        var definition = new ObjectTypeConfiguration
        {
            Description = CollectionSegmentType_Description,
            RuntimeType = typeof(CollectionSegment)
        };

        definition.Fields.Add(new(
            Names.PageInfo,
            CollectionSegmentType_PageInfo_Description,
            TypeReference.Parse("CollectionSegmentInfo!"),
            pureResolver: GetPagingInfo));

        definition.Fields.Add(new(
            Names.Items,
            CollectionSegmentType_Items_Description,
            pureResolver: GetItems)
            { Flags = CoreFieldFlags.CollectionSegmentItemsField });

        if (withTotalCount)
        {
            definition.Fields.Add(new(
                Names.TotalCount,
                type: TypeReference.Parse($"{ScalarNames.Int}!"),
                pureResolver: GetTotalCount)
            {
                Flags = CoreFieldFlags.TotalCount
            });
        }

        return definition;
    }

    private static IPageInfo GetPagingInfo(IResolverContext context)
        => context.Parent<CollectionSegment>().Info;

    private static IEnumerable<object?> GetItems(IResolverContext context)
        => context.Parent<CollectionSegment>().Items;

    private static object GetTotalCount(IResolverContext context)
        => context.Parent<CollectionSegment>().TotalCount;

    private static bool IsItemsField(ObjectFieldConfiguration field)
        => (field.Flags & CoreFieldFlags.CollectionSegmentItemsField) == CoreFieldFlags.CollectionSegmentItemsField;

    internal static class Names
    {
        public const string PageInfo = "pageInfo";
        public const string Items = "items";
        public const string TotalCount = "totalCount";
    }
}
