using System.Linq.Expressions;
using HotChocolate.Data.Projections.Expressions;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionScopeTests
{
    [Fact]
    public void CreateMemberInit_Should_MergeSelections_When_MemberHasThreeAssignments()
    {
        // arrange
        var source = new Item
        {
            Id = 1,
            Name = "Parent",
            Child = new Item { Id = 2, Name = "Child", Values = [3, 4] }
        };
        Expression<Func<Item>>[] projections =
        [
            () => new Item { Id = source.Id, Child = new Item { Name = source.Child!.Name } },
            () => new Item { Name = source.Name, Child = new Item { Id = source.Child!.Id } },
            () => new Item { Child = new Item { Values = source.Child!.Values } }
        ];
        var scope = new QueryableProjectionScope(typeof(Item), "item");

        foreach (var projection in projections)
        {
            foreach (var binding in ((MemberInitExpression)projection.Body).Bindings)
            {
                scope.Level.Peek().Enqueue((MemberAssignment)binding);
            }
        }

        // act
        var expression = (MemberInitExpression)scope.CreateMemberInit();
        var result = Expression.Lambda<Func<Item>>(expression).Compile()();

        // assert
        new
        {
            Members = expression.Bindings.Select(t => t.Member.Name).ToArray(),
            result.Id,
            result.Name,
            Child = new { result.Child!.Id, result.Child.Name, result.Child.Values }
        }.MatchInlineSnapshot(
            """
            {
              "Members": [
                "Id",
                "Child",
                "Name"
              ],
              "Id": 1,
              "Name": "Parent",
              "Child": {
                "Id": 2,
                "Name": "Child",
                "Values": [
                  3,
                  4
                ]
              }
            }
            """);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateMemberInit_Should_PreserveBindings_When_CollectionSourcesDiffer(bool filterFirst)
    {
        // arrange
        var source = new Item { Values = [1, 2, 3] };
        Expression<Func<Item>> first = filterFirst
            ? () => new Item { Values = source.Values.Where(x => x > 1).ToArray() }
            : () => new Item { Values = source.Values };
        Expression<Func<Item>> second = () => new Item { Values = source.Values.Where(x => x > 2).ToArray() };
        var firstBinding = (MemberAssignment)((MemberInitExpression)first.Body).Bindings[0];
        var secondBinding = (MemberAssignment)((MemberInitExpression)second.Body).Bindings[0];
        var scope = new QueryableProjectionScope(typeof(Item), "item");
        scope.Level.Peek().Enqueue(firstBinding);
        scope.Level.Peek().Enqueue(secondBinding);

        // act
        var expression = (MemberInitExpression)scope.CreateMemberInit();

        // assert
        Assert.Collection(
            expression.Bindings,
            binding => Assert.Same(firstBinding, binding),
            binding => Assert.Same(secondBinding, binding));
    }

    public class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public Item? Child { get; set; }

        public int[] Values { get; set; } = [];
    }
}
