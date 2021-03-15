using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Filtering.Lists
{
    public class Neo4JListFilterTests
        : SchemaCache
        , IClassFixture<Neo4jResource>
    {

        private readonly string _fooEntities = @"
            CREATE (a:Foo {BarString: 'a'})-[:RELATED_TO]->(:FooNested {Bar: 'a'}),
                    (a)-[:RELATED_TO]->(:FooNested {Bar: 'a'}),
                    (a)-[:RELATED_TO]->(:FooNested {Bar: 'a'}),
                    (b:Foo {BarString: 'b'})-[:RELATED_TO]->(:FooNested {Bar: 'c'}),
                    (b)-[:RELATED_TO]->(:FooNested {Bar: 'a'}),
                    (b)-[:RELATED_TO]->(:FooNested {Bar: 'a'}),
                    (c:Foo {BarString: 'c'})-[:RELATED_TO]->(:FooNested {Bar: 'a'}),
                    (c)-[:RELATED_TO]->(:FooNested {Bar: 'd'}),
                    (c)-[:RELATED_TO]->(:FooNested {Bar: 'b'}),
                    (d:Foo {BarString: 'd'})-[:RELATED_TO]->(:FooNested {Bar: 'c'}),
                    (d)-[:RELATED_TO]->(:FooNested {Bar: 'd'}),
                    (d)-[:RELATED_TO]->(:FooNested {Bar: 'b'}),
                    (e:Foo {BarString: 'e'})-[:RELATED_TO]->(:FooNested),
                    (e)-[:RELATED_TO]->(:FooNested {Bar: 'd'}),
                    (e)-[:RELATED_TO]->(:FooNested {Bar: 'b'})
        ";

        public class Foo
        {
            public string BarString { get; set; }
            [Neo4JRelationship("RELATED_TO")]
            public List<FooNested> FooNested { get; set; }
        }

        public class FooNested
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.FooNested);
            }
        }

        public Neo4JListFilterTests(Neo4jResource neo4JResource)
        {
            Init(neo4JResource);
        }

        [Fact]
        public async Task Create_ArraySomeObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooFilterType>(_fooEntities,false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { some: { bar: { eq: \"a\" }}}}){ fooNested { bar }}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");
        }

        [Fact]
        public async Task Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooFilterType>(_fooEntities,false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        "{ root(where: { fooNested: { all: { bar: { eq: \"a\" }}}}){ fooNested { bar }}}")
                    .Create());

            res1.MatchDocumentSnapshot("a");
        }

    }
}
