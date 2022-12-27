using HotChocolate.Data.Filters;
using HotChocolate.Types;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class StringFilterTests : FilterTestBase<StringFilterTests.Term, StringFilterTests.TermFilterType>
{
    private readonly Term[] _data =
    {
        new Term
        {
            Value = "Query",
            Description = "The query type has fields to get data."
        },
        new Term
        {
            Value = "Mutation",
            Description = "The mutation type has several operations to manipulate data."
        },
        new Term
        {
            Value = "Subscription",
            Description = "The subscription type can be used to directly notify on changes"
        }
    };

    private const string Selection = "value description";

    /// <inheritdoc />
    protected override IReadOnlyList<Term> Data => _data;

    /// <inheritdoc />
    public StringFilterTests(ElasticsearchResource resource) : base(resource)
    {
    }

    [Fact]
    public async Task ElasticSearch_String_StartsWith()
    {
        var result = await ExecuteFilterTest(@"description: { startsWith: ""mutat""}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_String_EndsWith()
    {
        var result = await ExecuteFilterTest(@"description: { endsWith: ""nges""}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_String_Contains()
    {
        var result = await ExecuteFilterTest(@"description: { contains: ""ields""}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_String_NotStartsWith()
    {
        var result = await ExecuteFilterTest(@"description: { nstartsWith: ""mutat""}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_String_NotEndsWith()
    {
        var result = await ExecuteFilterTest(@"description: { nendsWith: ""nges""}", Selection);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_String_NotContains()
    {
        var result = await ExecuteFilterTest(@"description: { ncontains: ""ields""}", Selection);
        result.MatchQuerySnapshot();
    }

    public class Term
    {
        public string Value { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public class TermFilterType : FilterInputType<Term>
    {
        /// <inheritdoc />
        protected override void Configure(IFilterInputTypeDescriptor<Term> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Value).Type<StringFilterType>();
            descriptor.Field(x => x.Description).Type<StringFilterType>();
        }
    }

    public class StringFilterType : StringOperationFilterInputType
    {
        /// <inheritdoc />
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotStartsWith).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.EndsWith).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotEndsWith).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
            descriptor.Operation(DefaultFilterOperations.NotContains).Type<StringType>();
        }
    }
}

