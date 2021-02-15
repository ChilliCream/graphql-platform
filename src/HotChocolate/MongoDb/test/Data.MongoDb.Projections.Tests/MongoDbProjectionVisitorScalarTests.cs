using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Projections
{
    public class MongoDbProjectionVisitorScalarTests
        : IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true, Baz = "a" },
            new Foo { Bar = false, Baz = "b" }
        };

        private readonly SchemaCache _cache;

        public MongoDbProjectionVisitorScalarTests(MongoResource resource)
        {
            _cache = new SchemaCache(resource);
        }

        [Fact]
        public async Task Create_ProjectsTwoProperties_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ bar baz }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_Expression()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ baz }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_WithResolver()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                objectType: new ObjectType<Foo>(
                    x => x
                        .Field("foo")
                        .Resolver(
                            new[]
                            {
                                "foo"
                            })
                        .Type<ListType<StringType>>()));

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ baz foo }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public bool Bar { get; set; }

            public string Baz { get; set; }
        }
    }
}
