using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class FilterContextTests
{
    [Fact]
    public async Task GetFields_Should_ReturnScalarField()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(t => t
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(ctx =>
                {
                    context = ctx.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
                test(where: { title: { eq: "test" } }) {
                    title
                }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        var field = Assert.Single(context.GetFields());
        Assert.Empty(context.GetOperations());
        var operation = Assert.Single(Assert.IsType<FilterInfo>(field.Value).GetOperations());
        Assert.Empty(Assert.IsType<FilterInfo>(field.Value).GetFields());
        Assert.Equal("title", field.Field.Name);
        Assert.Equal("eq", operation.Field.Name);
        Assert.Equal("test", Assert.IsType<FilterValue>(operation.Value).Value);
    }

    [Fact]
    public async Task When_Query_Is_Empty_IsDefined_Should_Be_False()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(t => t
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(ctx =>
                {
                    context = ctx.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
              test {
                title
              }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        Assert.False(context.IsDefined);
    }

    [Fact]
    public async Task When_Query_Is_Set_IsDefined_Should_Be_False()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(t => t
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(ctx =>
                {
                    context = ctx.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
              test(where: { title: { eq: "test" } }) {
                title
              }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        Assert.True(context.IsDefined);
    }

    [Fact]
    public async Task GetFields_Should_ReturnScalarFieldWithListOperation()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(t => t
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(ctx =>
                {
                    context = ctx.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
                test(where: { title: { in: ["a", "b"] } }) {
                    title
                }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        var field = Assert.Single(context!.GetFields());
        Assert.Empty(context.GetOperations());
        var operation =
            Assert.Single(Assert.IsType<FilterInfo>(field.Value).GetOperations());
        Assert.Empty(Assert.IsType<FilterInfo>(field.Value).GetFields());
        Assert.Equal("title", field.Field.Name);
        Assert.Equal("in", operation.Field.Name);
        var value = Assert.IsType<FilterValue>(operation.Value).Value as IEnumerable<string>;
        Assert.Equal("a", value!.FirstOrDefault());
        Assert.Equal("b", value!.LastOrDefault());
    }

    [Fact]
    public async Task GetFields_Should_ReturnFilterValueCollectionWhenOr()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    context = x.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
                test(where: {
                    or: [
                        { title: { eq: "a" } }
                        { title: { eq: "b" } }
                    ]
                }) {
                    title
                }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        var operation = Assert.Single(context.GetOperations());
        Assert.Empty(context.GetFields());
        var valueCollection = Assert.IsType<FilterValueCollection>(operation.Value);
        var field0 = Assert.Single(Assert.IsType<FilterInfo>(valueCollection[0]).GetFields());
        Assert.Equal("title", field0.Field.Name);
        var operation0 = Assert.Single(Assert.IsType<FilterInfo>(field0.Value).GetOperations());
        Assert.Equal("eq", operation0.Field.Name);
        Assert.Equal("a", Assert.IsType<FilterValue>(operation0.Value).Value);
        var field1 = Assert.Single(Assert.IsType<FilterInfo>(valueCollection[1]).GetFields());
        Assert.Equal("title", field1.Field.Name);
        var operation1 = Assert.Single(Assert.IsType<FilterInfo>(field1.Value).GetOperations());
        Assert.Equal("eq", operation1.Field.Name);
        Assert.Equal("b", Assert.IsType<FilterValue>(operation1.Value).Value);
    }

    [Fact]
    public async Task GetFields_Should_ReturnDeepObject()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(t => t
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(ctx =>
                {
                    context = ctx.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
                test(where: { author: { name: { eq: "test" } } }) {
                    title
                }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        var author = Assert.Single(context.GetFields());
        Assert.Empty(context.GetOperations());
        var name = Assert.Single(Assert.IsType<FilterInfo>(author.Value).GetFields());
        Assert.Empty(Assert.IsType<FilterInfo>(author.Value).GetOperations());
        var operation =
            Assert.Single(Assert.IsType<FilterInfo>(name.Value).GetOperations());
        Assert.Empty(Assert.IsType<FilterInfo>(name.Value).GetFields());
        Assert.Equal("author", author.Field.Name);
        Assert.Equal("name", name.Field.Name);
        Assert.Equal("eq", operation.Field.Name);
        Assert.Equal("test", Assert.IsType<FilterValue>(operation.Value).Value);
    }

    [Fact]
    public async Task GetFields_Should_ReturnOrOperations()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(t => t
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(ctx =>
                {
                    context = ctx.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
                test(where: {
                    and: [
                        {
                            title: {
                                in: ["a", "b"]
                            }
                            author: {
                                name: {
                                    eq: "test"
                                    neq: "test"
                                }
                            }
                        }
                        { pages: { eq: 1 } }
                        { isActive: { eq: true } }
                    ],
                    or: [
                        { title: { eq: "a" } }
                        { title: { eq: "b" } }
                    ]
                }) {
                    title
                }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        context.ToDictionary().MatchSnapshot();
    }

    [Fact]
    public async Task Handled_Should_EnableFilterExecution()
    {
        IImmutableDictionary<string, object?>? localContextData = null;
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    x.GetFilterContext()?.Handled(false);
                    localContextData = x.LocalContextData.Add("foo", true);

                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
                test(where: {
                    title: {
                        in: ["a", "b"]
                    }
                    author: {
                        name: {
                            eq: "test"
                            neq: "test"
                        }
                    }
                }) {
                    title
                }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(localContextData);
        Assert.False(localContextData.ContainsKey(QueryableFilterProvider.SkipFilteringKey));
    }

    [Fact]
    public async Task Handled_Should_DisableFilterExecutionByDefault()
    {
        IImmutableDictionary<string, object?>? localContextData = null;
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    x.GetFilterContext();
                    localContextData = x.LocalContextData.Add("foo", true);

                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query =
            """
            {
                test(where: {
                    title: {
                        in: ["a", "b"]
                        eq: null
                    }
                    author: {
                        name: {
                            eq: "test"
                            neq: "test"
                        }
                    }
                }) {
                    title
                }
            }
            """;

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(localContextData);
        Assert.True(localContextData.ContainsKey(QueryableFilterProvider.SkipFilteringKey));
    }

    [Fact]
    public async Task GetFilterContext_ReturnNullWhenNoFiltering()
    {
        var obj = new object();

        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .Resolve(x =>
                {
                    obj = x.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query = @"
            {
                test {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.Null(obj);
    }

    [Fact]
    public async Task FilterContext_Should_NotFail_When_FilterArgumentIsNotProvided()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    context = x.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        const string query = @"
            {
                test {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(context);
        context.ToDictionary().MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Report_Resolver_Path_When_Filter_Value_Cannot_Be_Parsed()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(t => t
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Avatar>>>()
                .UseFiltering<AvatarFilterInputType>()
                .Resolve(ctx =>
                {
                    var context = ctx.GetFilterContext()!;
                    var field = Assert.Single(context.GetFields());
                    var operation = Assert.Single(Assert.IsType<FilterInfo>(field.Value).GetOperations());
                    _ = Assert.IsType<FilterValue>(operation.Value).Value;
                    return Array.Empty<Avatar>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                test(where: { url: { eq: "" } }) {
                    url
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        // the input path is not asserted as FilterValue parses without an input path.
        var error = Assert.Single(result.ExpectOperationResult().Errors);
        Assert.Equal("The value is not a valid image data URL.", error.Message);
        Assert.Equal(["test"], error.Path!.ToList());
        Assert.Null(error.Locations);
        Assert.Equal("ImageDataUrl", Assert.Contains("fieldType", error.Extensions!));
    }

    public class Book
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public int Pages { get; set; }

        public int Chapters { get; set; }

        public bool IsActive { get; set; }

        public Author? Author { get; set; }

        public Author[]? CoAuthor { get; set; }
    }

    public class Author
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    public class Avatar
    {
        public string? Url { get; set; }
    }

    public class AvatarFilterInputType : FilterInputType<Avatar>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Avatar> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Url).Type<ImageDataUrlOperationFilterInputType>();
        }
    }

    public class ImageDataUrlOperationFilterInputType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<ImageDataUrlType>();
        }
    }

    public class ImageDataUrlType : ScalarType<string, StringValueNode>
    {
        public ImageDataUrlType()
            : base("ImageDataUrl")
        {
        }

        protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
        {
            return Validate(valueLiteral.Value);
        }

        protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        {
            return Validate(inputValue.GetString()!);
        }

        protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
        {
            resultValue.SetStringValue(Validate(runtimeValue));
        }

        protected override StringValueNode OnValueToLiteral(string runtimeValue)
        {
            return new StringValueNode(Validate(runtimeValue));
        }

        private string Validate(string runtimeValue)
        {
            if (!runtimeValue.StartsWith("data:image/", StringComparison.Ordinal))
            {
                throw new LeafCoercionException("The value is not a valid image data URL.", this);
            }

            return runtimeValue;
        }
    }
}
