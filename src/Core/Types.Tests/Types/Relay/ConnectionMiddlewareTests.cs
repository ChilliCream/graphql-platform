using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class ConnectionMiddlewareTests
    {
        [Fact]
        public async Task ExecuteQueryWithConnectionMiddleware_ShouldHandleVariousReturnTypes()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<SomeQuery>();
                t.RegisterType<FooType>();
                t.UseGlobalObjectIdentifier();
            });

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync(
                    @"{
                        foosAsIConnectionAndIEnumerable(first: 2) {
                            totalCount
                            pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                            nodes { id string }
                        }
                        foosAsIEnumerable(first: 2) {
                            totalCount
                            pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                            nodes { id string }
                        }
                        foosAsIQueryable(first: 2) {
                            totalCount
                            pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                            nodes { id string }
                        }
                    }");

            // assert
            result.MatchSnapshot();
        }

        public class SomeQuery
        {
            [UsePaging(SchemaType = typeof(FooType))]
            public FooConnection GetFoosAsIConnectionAndIEnumerable()
            {
                // Values returned here in this resolver should be considered as-is

                var edges = Foos.Select(f => new Edge<Foo>(f, f.Id.ToString())).ToList();

                var pageInfo = new PageInfo(
                    hasNextPage: true,
                    hasPreviousPage: true,
                    startCursor: edges.First().Cursor,
                    endCursor: edges.Last().Cursor,
                    totalCount: 100);

                return new FooConnection(pageInfo, edges);
            }

            [UsePaging(SchemaType = typeof(FooType))]
            public IEnumerable<Foo> GetFoosAsIEnumerable()
                => Foos;

            [UsePaging(SchemaType = typeof(FooType))]
            public IQueryable<Foo> GetFoosAsIQueryable()
                => Foos.AsQueryable();
        }

        private static IEnumerable<Foo> Foos => new List<Foo>
        {
            new Foo(1),
            new Foo(2),
            new Foo(3),
            new Foo(4)
        };

        public class FooConnection : IConnection, IEnumerable<Foo>
        {
            public IPageInfo PageInfo { get; private set; }

            public IReadOnlyList<IEdge> Edges { get; private set; }

            public IEnumerator<Foo> GetEnumerator() => Edges.Select(e => e.Node).Cast<Foo>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public FooConnection(IPageInfo pageInfo, IReadOnlyList<IEdge> edges)
            {
                PageInfo = pageInfo;
                Edges = edges;
            }
        }

        public class Foo
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public int Id { get; private set; }

            public string GetString() => "Hello";

            public Foo(int id) => Id = id;
        }

        public class FooType : ObjectType<Foo>
        {
            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
                => descriptor.AsNode().IdField(f => f.Id);
        }
    }
}
