using System.Linq.Expressions;

namespace GreenDonut.Data;

public class GreenDonutQueryableExtensionsTests
{
    private static readonly Shelf[] s_shelves =
    [
        new("second", 1, new Book("B", 20)),
        new("empty", 1, null),
        new("first", 0, new Book("A", 10))
    ];

    [Fact]
    public void OrderBy_Should_SortNullNavigationAsDefault_When_QueryableIsInMemory()
    {
        // arrange
        var sortDefinition = new SortDefinition<Shelf>().AddAscending(t => t.Book!.Title);

        // act
        var names = s_shelves.AsQueryable().OrderBy(sortDefinition).Select(t => t.Name).ToArray();

        // assert
        Assert.Equal(["empty", "first", "second"], names);
    }

    [Fact]
    public void ThenBy_Should_SortNullNavigationAsDefault_When_QueryableIsInMemory()
    {
        // arrange
        var sortDefinition = new SortDefinition<Shelf>().AddDescending(t => t.Book!.Pages);

        // act
        var names = s_shelves
            .AsQueryable()
            .OrderBy(t => t.Rank)
            .ThenBy(sortDefinition)
            .Select(t => t.Name)
            .ToArray();

        // assert
        Assert.Equal(["first", "second", "empty"], names);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderBy_Should_UseCustomComparer_When_SortHasNestedKey(bool thenBy)
    {
        // arrange
        Shelf[] shelves =
        [
            new("first", 0, new Book("A", 10)),
            new("second", 0, new Book("B", 20))
        ];
        var query = shelves.AsQueryable();
        var definition = new SortDefinition<Shelf>(new CustomSort());

        // act
        var names = (thenBy
                ? query.OrderBy(t => t.Rank).ThenBy(definition)
                : query.OrderBy(definition))
            .Select(t => t.Name)
            .ToArray();

        // assert
        Assert.Equal(["second", "first"], names);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderBy_Should_EvaluateNavigationOnce_When_KeyHasNestedMembers(bool thenBy)
    {
        // arrange
        var leaf = new CountedNode("A", null);
        var third = new CountedNode("third", leaf);
        var second = new CountedNode("second", third);
        var root = new CountedNode("root", second);
        var query = new[] { root, root }.AsQueryable();
        var definition = new SortDefinition<CountedNode>()
            .AddAscending(t => t.Next!.Next!.Next!.Name);

        // act
        _ = (thenBy
                ? query.OrderBy(t => t.Name).ThenBy(definition)
                : query.OrderBy(definition))
            .ToArray();

        // assert
        Assert.Equal<int>([2, 2, 2], [root.Reads, second.Reads, third.Reads]);
    }

    [Fact]
    public void OrderBy_Should_PreserveConditionalBranches_When_SelectorHandlesNull()
    {
        // arrange
        var definition = new SortDefinition<Shelf>()
            .AddAscending(t => t.Book == null ? "Z" : t.Book.Title);

        // act
        var names = s_shelves.AsQueryable().OrderBy(definition).Select(t => t.Name).ToArray();

        // assert
        Assert.Equal(["first", "second", "empty"], names);
    }

    [Fact]
    public void OrderBy_Should_PreserveNullableHasValue_When_SelectorHandlesNull()
    {
        // arrange
        var values = new[] { new DatedItem(null), new DatedItem(new DateTime(2026, 1, 1)) };
        var definition = new SortDefinition<DatedItem>()
            .AddAscending(t => t.Date.HasValue ? t.Date.Value.Year : 9999);

        // act
        var dates = values.AsQueryable().OrderBy(definition).Select(t => t.Date).ToArray();

        // assert
        Assert.Equal<DateTime?>([new DateTime(2026, 1, 1), null], dates);
    }

    public record DatedItem(DateTime? Date);

    public record Shelf(string Name, int Rank, Book? Book);

    public record Book(string Title, int Pages);

    public class CountedNode(string name, CountedNode? next)
    {
        public string Name { get; } = name;

        public int Reads { get; private set; }

        public CountedNode? Next
        {
            get
            {
                Reads++;
                return next;
            }
        }
    }

    private sealed class CustomSort : ISortBy<Shelf>
    {
        private static readonly Expression<Func<Shelf, string>> s_key = t => t.Book!.Title;
        private static readonly IComparer<string> s_comparer =
            Comparer<string>.Create((a, b) => StringComparer.Ordinal.Compare(b, a));

        public LambdaExpression KeySelector => s_key;

        public bool Ascending => true;

        public IOrderedQueryable<Shelf> ApplyOrderBy(IQueryable<Shelf> queryable)
            => queryable.OrderBy(s_key, s_comparer);

        public IOrderedQueryable<Shelf> ApplyThenBy(IOrderedQueryable<Shelf> queryable)
            => queryable.ThenBy(s_key, s_comparer);
    }
}
