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
        , IClassFixture<Neo4jResource<Neo4JConfig>>
    {
        private readonly string _fooEntities = @"
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

        public Neo4JListFilterTests(Neo4jResource<Neo4JConfig> neo4JResource)
        {
            Init(neo4JResource);
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
                        "{ root(where: { barString: {eq: \"a\" } fooNested: { all: { bar: { eq: \"a\" }}}}){ barString fooNested { bar barNested { foo } }}}")
                    .Create());

            res1.MatchDocumentSnapshot("all");
        }
    }
}
