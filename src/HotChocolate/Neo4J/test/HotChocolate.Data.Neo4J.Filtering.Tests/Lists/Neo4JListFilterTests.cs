using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Neo4J.Filtering.Lists
{
    public class Neo4JListFilterTests
        : IClassFixture<Neo4JFixture>
    {
        private readonly Neo4JFixture _fixture;

        public Neo4JListFilterTests(Neo4JFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly string _fooEntitiesCypher = @"
            CREATE (a:Foo {BarString: 'a'})-[:RELATED_FOO]->(:FooNested {Bar: 'a'})-[:RELATED_BAR]->(:BarNested {Foo: 'a'}),
                    (a)-[:RELATED_FOO]->(:FooNested {Bar: 'a'})-[:RELATED_BAR]->(:BarNested {Foo: 'a'}),
                    (a)-[:RELATED_FOO]->(:FooNested {Bar: 'a'})-[:RELATED_BAR]->(:BarNested {Foo: 'a'}),
                    (b:Foo {BarString: 'b'})-[:RELATED_FOO]->(:FooNested {Bar: 'c'}),
                    (b)-[:RELATED_FOO]->(:FooNested {Bar: 'a'}),
                    (b)-[:RELATED_FOO]->(:FooNested {Bar: 'a'}),
                    (c:Foo {BarString: 'c'})-[:RELATED_FOO]->(:FooNested {Bar: 'a'}),
                    (c)-[:RELATED_FOO]->(:FooNested {Bar: 'd'}),
                    (c)-[:RELATED_FOO]->(:FooNested {Bar: 'b'}),
                    (d:Foo {BarString: 'd'})-[:RELATED_FOO]->(:FooNested {Bar: 'c'}),
                    (d)-[:RELATED_FOO]->(:FooNested {Bar: 'd'}),
                    (d)-[:RELATED_FOO]->(:FooNested {Bar: 'b'}),
                    (e:Foo {BarString: 'e'})-[:RELATED_FOO]->(:FooNested),
                    (e)-[:RELATED_FOO]->(:FooNested {Bar: 'd'}),
                    (e)-[:RELATED_FOO]->(:FooNested {Bar: 'b'})
        ";

        public class Foo
        {
            public string BarString { get; set; }

            [Neo4JRelationship("RELATED_FOO")]
            public List<FooNested> FooNested { get; set; }
        }

        public class FooNested
        {
            public string? Bar { get; set; }

            [Neo4JRelationship("RELATED_BAR")]
            public List<BarNested> BarNested { get; set; }
        }

        public class BarNested
        {
            public string? Foo { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        [Fact]
        public async Task Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester =
                await _fixture.GetOrCreateSchema<Foo, FooFilterType>(_fooEntitiesCypher);

            // act
            // assert
            const string query1 =
                @"{
                    root(where: {
                        barString: {
                            eq: ""a""
                        }
                        fooNested: {
                            all: {
                                bar: { eq: ""a"" }
                            }
                        }
                    }){
                        barString
                        fooNested {
                            bar
                            barNested {
                                foo
                            }
                        }
                    }
                }";

            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query1)
                    .Create());

            res1.MatchDocumentSnapshot("all");
        }
    }
}
