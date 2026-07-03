using System.Linq.Expressions;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class GlobalFieldMiddlewareProjectionTests
{
    [Fact]
    public async Task AsSelector_Should_ProjectMemberFields_When_GlobalFieldMiddlewareIsApplied()
    {
        // arrange
        var captured = new List<Expression<Func<Item, Item>>>();
        var executor = await new ServiceCollection()
            .AddSingleton(captured)
            .AddGraphQL()
            .AddQueryType<Query>()
            .UseField(next => next)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                name
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var selector = Assert.Single(captured);
        var memberInit = Assert.IsType<MemberInitExpression>(selector.Body);
        Assert.Contains(memberInit.Bindings, b => b.Member.Name == nameof(Item.Name));
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "name": "Foo"
                  }
                ]
              }
            }
            """);
    }

    public class Query
    {
        public IQueryable<Item> GetItems(
            ISelection selection,
            [Service] List<Expression<Func<Item, Item>>> captured)
        {
            var selector = selection.AsSelector<Item>();
            captured.Add(selector);
            return new[] { new Item { Id = 1, Name = "Foo" } }.AsQueryable().Select(selector);
        }
    }

    public class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }
}
