using System;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Execution
{
    public class QueryExecuterTests
    {
        [Fact]
        public void SchemaIsNull_ShouldThrow()
        {
            // act
            Action a = () => new QueryExecuter(null);
            Action b = () => new QueryExecuter(null, 0);

            // assert
            Assert.Throws<ArgumentNullException>(a);
            Assert.Throws<ArgumentNullException>(b);
        }

        [Fact]
        public void InitialCacheSize_100()
        {
            // arrange
            Schema schema = CreateSchema();

            // act
            QueryExecuter executer = new QueryExecuter(schema);

            // assert
            Assert.Equal(100, executer.CacheSize);
            Assert.Equal(0, executer.CachedOperations);
        }

        [Fact]
        public async Task ExecuteShortHandQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryExecuter executer = new QueryExecuter(schema);
            QueryRequest request = new QueryRequest("{ a }");

            // act
            QueryResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task ExecuteCachedOperation()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryExecuter executer = new QueryExecuter(schema);
            QueryRequest requesta = new QueryRequest("{ a }");
            QueryRequest requestb = new QueryRequest("{ b(a: \"foo\") }");

            // act
            QueryResult resulta1 = await executer.ExecuteAsync(requesta);
            QueryResult resultb = await executer.ExecuteAsync(requestb);
            QueryResult resulta2 = await executer.ExecuteAsync(requesta);

            // assert
            Assert.Equal(2, executer.CachedOperations);
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndNoOperationName_Error()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryExecuter executer = new QueryExecuter(schema);
            QueryRequest request = new QueryRequest(
                "query a { a } query b { b }");

            // act
            QueryResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndOperationName_NoErrors()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryExecuter executer = new QueryExecuter(schema);
            QueryRequest request = new QueryRequest(
                "query a { a } query b { b }", "a");

            // act
            QueryResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task ExecuteQueryWith2OperationsAndInvalidOperationName_Error()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryExecuter executer = new QueryExecuter(schema);
            QueryRequest request = new QueryRequest(
                "query a { a } query b { b }", "c");

            // act
            QueryResult result = await executer.ExecuteAsync(request);

            // assert
            Assert.NotNull(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query {
                    a: String
                    b(a: String!): String
                }
                ", c =>
            {
                c.BindResolver(() => "hello world a")
                    .To("Query", "a");
                c.BindResolver(ctx => "hello world " + ctx.Argument<string>("b"))
                    .To("Query", "b");
            });
        }
    }
}
