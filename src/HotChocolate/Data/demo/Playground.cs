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
        public int BooksWritten { get; set; }
    }

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType(d => d.Name("Query"))
                .AddType<Queries>()
                .Create(),
                new QueryExecutionOptions
                {
                    IncludeExceptionDetails = true
                });
        }
    }

    public class BookFilterType : FilterInputType<Book>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Book> descriptor)
        {
            // descriptor
            descriptor.Field(x => x.Author).Type<AuthorFilterInputType>();
            descriptor.Field(x => x.Pages).Type<FilterInputType<int>>();
            descriptor.Field(x => x.Name).Type<FilterInputType<string>>().AsMongo();
        }
    }

    public class AuthorFilterInputType : FilterInputType<Author>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Author> descriptor)
        {
            // descriptor
            descriptor.Field(x => x.Name).Type<BookFilterType>();
        }
    }

    // This one will not really exist
    public class IntFilterInputType : FilterInputType<int>
    {
        protected override void Configure(IFilterInputTypeDescriptor<int> descriptor)
        {
            // descriptor
            descriptor.Operation(Operations.Equals);
        }
    }
    // Operations for typeof(int): Equals

    public class StringFilterInputType : FilterInputType<string>
    {
        protected override void Configure(IFilterInputTypeDescriptor<string> descriptor)
        {
            // descriptor
            descriptor.Operation(Operations.Equals);
        }
    }
    // Convention(SQL) contains operation 'like'
    // Convention(MongoDb) not contains operations 'like'
    public class StringFieldConvention : FieldConvention
    {
        protected override void Configure(
            IFilterFieldConventionDescriptor descriptor)
        {
            descriptor.OfType(
                defintion => (
                    definition.Member is PropertyInfo info &&
                    info.Type == typeof(string)));
            descriptor.Operation(Operations.Equals);
        }
    }
}
