using System.Runtime.CompilerServices;
namespace HotChocolate.Data.Filters.Demo
{
    public class Book
    {
        public string Name { get; set; }
        public int Pages { get; set; }
        public Author Author { get; set; }
    }

    public class Author
    {
        public string Name { get; set; }
    }

    public class BookFilterType : FilterInputType<Book>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Book> descriptor)
        {
            // descriptor
            descriptor.Field(x => x.Author).Type<AuthorFilterInputType>();
            descriptor.Field(x => x.Pages).Type<FilterInputType<int>>();
            descriptor.Field(x => x.Name).Type<FilterInputType<string>>();
        }
    }
}
