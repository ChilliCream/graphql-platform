using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zeus.Abstractions;
using Zeus.Resolvers;
using Xunit;

namespace Zeus.Execution
{
    public class DocumentExecuterTests
    {
        [Fact]
        public async Task SimpleQuery()
        {
            // arrange
            string expectedValue = Guid.NewGuid().ToString();
            ISchema schema = Schema.Create(
                @"
                type Query {
                    foo: String!
                }
                ",
                b => b.Add("Query", "foo", () => expectedValue));
            string query =
                @"
                {
                    foo
                }
                ";

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Equal(expectedValue, result.Data["foo"]);
        }

        [Fact]
        public async Task SimpleQueryWithArgumentWithDefault()
        {
            // arrange
            ISchema schema = Schema.Create(
                @"
                type Query {
                    foo(s: String! = ""x""): String!
                }
                ",
                b => b.Add("Query", "foo", c => c.Argument<string>("s")));
            string query =
                @"
                {
                    foo
                }
                ";

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Equal("x", result.Data["foo"]);
        }

        [Fact]
        public async Task SimpleQueryWithArgumentWithValue()
        {
            // arrange
            ISchema schema = Schema.Create(
                @"
                type Query {
                    foo(s: String! = ""x""): String!
                }
                ",
                b => b.Add("Query", "foo", c => c.Argument<string>("s")));
            string query =
                @"
                {
                    foo(s: ""z"")
                }
                ";

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Equal("z", result.Data["foo"]);
        }

        [Fact]
        public async Task SimpleQueryWithArgumentWithVariable()
        {
            // arrange
            ISchema schema = Schema.Create(
                @"
                type Query {
                    foo(s: String! = ""x""): String!
                }
                ",
                b => b.Add("Query", "foo", c => c.Argument<string>("s")));
            string query =
                @"
                query f($v: String!){
                    foo(s: $v)
                }
                ";

            Dictionary<string, object> variableValues = new Dictionary<string, object>
            {
                { "v", "y" }
            };

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, query, null, variableValues, null, CancellationToken.None);

            // assert
            Assert.NotNull(result.Data);
            Assert.Null(result.Errors);
            Assert.Equal("y", result.Data["foo"]);
        }
    }
}