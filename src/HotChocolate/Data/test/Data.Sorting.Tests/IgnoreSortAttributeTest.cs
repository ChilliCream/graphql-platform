using System.Linq;
using CookieCrumble;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data;

public class IgnoreSortAttributeTest
{
    [Fact]
    public void Then_Ignore_Sort_Attribute_Removes_Sort_Properties()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType<Query>()
            .Create();

        // assert
        schema.MatchSnapshot();
    }

    public class Query
    {
        [UseSorting]
        public IQueryable<IgnoredSortProperty> GetFoos() => Enumerable.Empty<IgnoredSortProperty>().AsQueryable();
    }

    public class IgnoredSortProperty
    {
        public string Name { get; set; } = string.Empty;

        [GraphQLIgnoreSort]
        public int Bar { get; set; }

        public IgnoredSortType? Baz { get; set; }
    }

    [GraphQLIgnoreSort]
    public class IgnoredSortType
    {
        public string Foo { get; set; } = string.Empty;
    }
}
