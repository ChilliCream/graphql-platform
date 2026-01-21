using HotChocolate.Data.Filters;
using HotChocolate.Types;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class InTests : FilterTestBase<InTests.Foo, InTests.FooFilter>
{
    private readonly Foo[] _data = new[]
    {
        new Foo
        {
            Id = 1,
            TextId = "1"
        },
        new Foo
        {
            Id = 2,
            TextId = "2"
        },
        new Foo
        {
            Id = 3,
            TextId = "3"
        },
    };

    private const string Selection = "id textId";

    public class Foo
    {
        public int Id { get; set; }

        public string TextId { get; set; } = string.Empty;
    }

    public class FooFilter : FilterInputType<Foo>
    {
        /// <inheritdoc />
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Id).Type<NumberFilter>();
            descriptor.Field(x => x.TextId).Type<StringFilter>();
        }
    }

    public class NumberFilter : IntOperationFilterInputType
    {
        /// <inheritdoc />
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.In).Type<ListType<IntType>>();
            descriptor.Operation(DefaultFilterOperations.NotIn).Type<ListType<IntType>>();
        }
    }

    public class StringFilter : StringOperationFilterInputType
    {
        /// <inheritdoc />
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.In).Type<ListType<StringType>>();
            descriptor.Operation(DefaultFilterOperations.NotIn).Type<ListType<StringType>>();
        }
    }

    /// <inheritdoc />
    public InTests(ElasticsearchResource resource) : base(resource)
    {
    }

    [Fact]
    public async Task ElasticSearch_String_In()
    {
        var result = await ExecuteFilterTest(@"textId: { in: [""2"", ""3""]}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_String_NotIn()
    {
        var result = await ExecuteFilterTest(@"textId: { nin: [""2"", ""3""]}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Number_In()
    {
        var result = await ExecuteFilterTest(@"id: { in: [2, 3]}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Number_NotIn()
    {
        var result = await ExecuteFilterTest(@"id: { nin: [2, 3]}", Selection);
        result.MatchQuerySnapshot();
    }


    /// <inheritdoc />
    protected override IReadOnlyList<Foo> Data => _data;
}
