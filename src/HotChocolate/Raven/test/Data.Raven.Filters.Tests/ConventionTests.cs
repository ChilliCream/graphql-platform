namespace HotChocolate.Data.Filters;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class ConventionTests
{
    private readonly SchemaCache _cache;

    public ConventionTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task ListType_Should_NotContainAllOperation()
    {
        var tester =
            _cache.CreateSchema<TypeWithList, FilterInputType<TypeWithList>>(TypeWithList.Data);

        await Snapshot
            .Create()
            .Add(tester.Schema.Print(), "schema")
            .MatchAsync();
    }

    public class TypeWithList
    {
        public static TypeWithList[] Data =
        [
            new() { List = new List<ListItem>() { new() { Foo = "Foo", }, new() { Foo = "Bar", }, }, },
        ];

        public string? Id { get; set; }

        public List<ListItem>? List { get; set; }
    }

    public class ListItem
    {
        public string? Id { get; set; }

        public string? Foo { get; set; }
    }
}
