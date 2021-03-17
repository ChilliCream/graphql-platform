using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class SortMiddlewareTests
    {
        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[]
                {
                    new[]
                    {
                        new Foo {Bar = "baz"}, new Foo {Bar = "qux"}, new Foo {Bar = "quux"}
                    },
                    "AsEnumerable"
                },
                new object[]
                {
                    new[] {new Foo {Bar = "baz"}, new Foo {Bar = "qux"}, new Foo {Bar = "quux"}}
                        .AsQueryable(),
                    "AsQueryable"
                }
            };

        private static void AddField<T>(IObjectTypeDescriptor ctx, T resolvedItems)
        {
            ctx.Field("foo")
                .Resolver(resolvedItems)
                .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
                .UseSorting();
        }

        [MemberData(nameof(Data))]
        [Theory]
        public void InvokeAsync(object resolvedItems, string scenarioName)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(ctx =>
                {
                    if (resolvedItems is IQueryable<Foo> queryable)
                    {
                        AddField(ctx, queryable);
                    }
                    else if (resolvedItems is IEnumerable<Foo> enumerable)
                    {
                        AddField(ctx, enumerable);
                    }
                })
                .Create();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo(order_by: { bar: DESC}) { bar } }")
                    .Create();

            // act
            IExecutionResult result = schema.MakeExecutable().Execute(request);

            // assert
            result.MatchSnapshot(scenarioName);
        }

        private class Foo
        {
            public string Bar { get; set; }
        }
    }
}
