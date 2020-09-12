namespace HotChocolate.Types.Pagination
{
    public class CollectionSegmentType<TSchemaType>
        : ObjectType<ICollectionSegment>
        where TSchemaType : class, IOutputType
    {
        protected override void Configure(IObjectTypeDescriptor<ICollectionSegment> descriptor)
        {
            descriptor
                .Name(dependency => $"{dependency.Name}CollectionSegment")
                .DependsOn<TSchemaType>();

            descriptor
                .Field(i => i.Nodes)
                .Type<ListType<TSchemaType>>();
        }
    }
}
