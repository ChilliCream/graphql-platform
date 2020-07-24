namespace HotChocolate.Data.Filters.Demo
{
    public class AuthorFilterInputType : FilterInputType<Author>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Author> descriptor)
        {
            descriptor.Field(x => x.Name).Type<StringFilterInputType>();
        }
    }
}
