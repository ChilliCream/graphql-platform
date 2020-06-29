namespace HotChocolate.Types.OffsetPaging
{
    public class CollectionSliceType<TSchemaType> : ObjectType<ICollectionSlice> where TSchemaType : class, IOutputType
    {
        protected override void Configure(IObjectTypeDescriptor<ICollectionSlice> descriptor)
        {
            descriptor.Name(dependency => $"CollectionSliceOf{dependency.Name}").DependsOn<TSchemaType>();
            descriptor.Field(i => i.Nodes).Type<ListType<TSchemaType>>();
        }
    }
}