namespace HotChocolate.Types.Pagination
{
    public class CollectionSegmentType<T>
        : ObjectType<CollectionSegment>
        where T : class, IOutputType
    {
        protected override void Configure(IObjectTypeDescriptor<CollectionSegment> descriptor)
        {
            descriptor
                .Name(dependency => $"{dependency.Name}CollectionSegment")
                .DependsOn<T>();

            descriptor
                .Field(i => i.Items)
                .Type<ListType<T>>();
        }
    }
}
