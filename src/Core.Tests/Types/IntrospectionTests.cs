using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Newtonsoft.Json;
using Xunit;

namespace HotChocolate.Types
{
    public class IntrospectionTests
    {
        [Fact]
        public async Task TypeNameIntrospectionOnQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse("{ __typename }");

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task TypeNameIntrospectionNotOnQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse("{ b { __typename } }");

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task TypeIntrospectionOnQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(
                "{ __type (type: \"Foo\") { name } }");

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task TypeIntrospectionOnQueryWithFields()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(
                "{ __type (type: \"Foo\") { name fields { name type { name } } } }");

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task ExecuteGraphiQLIntrospectionQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("IntrospectionQuery.graphql"));

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            Stopwatch sw = Stopwatch.StartNew();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(c => c.RegisterType<Query>());
        }

        private class Query
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("a")
                    .Type<StringType>()
                    .Resolver(() => "a");

                descriptor.Field("b")
                    .Type<Foo>()
                    .Resolver(() => new object());
            }
        }

        private class Foo
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("a")
                    .Type<StringType>()
                    .Resolver(() => "foo.a");
            }
        }
    }
}
