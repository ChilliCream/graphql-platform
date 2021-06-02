using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Paging
{
    public class Neo4JOffsetPagingTests
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        private const string FooEntitiesCypher = @"
            CREATE
                (:Foo {Bar: 'a'}),
                (:Foo {Bar: 'b'}),
                (:Foo {Bar: 'd'}),
                (:Foo {Bar: 'e'}),
                (:Foo {Bar: 'f'})
        ";

        private class Foo
        {
            public string Bar { get; set; } = default!;
        }

        public Neo4JOffsetPagingTests(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task OffsetPaging_SchemaSnapshot()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo>(FooEntitiesCypher);
            tester.Schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Simple_StringList_Default_Items()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo>(FooEntitiesCypher);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                @"{
                        root {
                            items {
                                bar
                            }
                            pageInfo {
                                hasNextPage
                                hasPreviousPage
                            }
                        }
                    }");

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task Simple_StringList_Take_2()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo>(FooEntitiesCypher);

            //act
            IExecutionResult result = await tester.ExecuteAsync(
                @"{
                            root(take: 2) {
                                items {
                                    bar
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                }
                            }
                        }");

            // assert
            result.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task Simple_StringList_Take_2_After()
        {
            // arrange
            IRequestExecutor tester = await _fixture.GetOrCreateSchema<Foo>(FooEntitiesCypher);

            // act
            IExecutionResult result = await tester.ExecuteAsync(
                @"{
                            root(take: 2 skip: 2) {
                                items {
                                    bar
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                }
                            }
                        }");

            // assert
            result.MatchDocumentSnapshot();
        }
    }
}
