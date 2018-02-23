
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zeus;
using Zeus.Abstractions;
using Zeus.Execution;
using Zeus.Parser;
using Zeus.Resolvers;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class BasicDocumentExecuterTests
    {

        [Fact]
        public async Task CallGetStringOnSimpleSchemaQuery()
        {
            // arrange
            string expectedResult = Guid.NewGuid().ToString("N");

            Schema schema = Schema.Create(
                @"
                type Query {
                    getString: String
                }
                ",

                ResolverBuilder.Create()
                    .Add("Query", "getString", () => expectedResult)
                    .Build()
            );

            QueryDocumentReader queryDocumentReader = new QueryDocumentReader();
            QueryDocument queryDocument = queryDocumentReader.Read(
                @"
                {
                    getString
                }
                "
            );

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, queryDocument, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("getString"));
            Assert.Equal(expectedResult, result.Data["getString"]);
        }

        [Fact]
        public async Task CallGetFooAndResolveObject()
        {
            // arrange
            string expectedResult = Guid.NewGuid().ToString("N");

            Schema schema = Schema.Create(
                @"
                type Foo
                {
                    a: String!
                    b: String
                    c: Int
                }

                type Query {
                    getFoo: Foo 
                }
                ",

                ResolverBuilder.Create()
                    .Add("Query", "getFoo", () => "something")
                    .Add("Foo", "a", () => "hello")
                    .Add("Foo", "b", () => "world")
                    .Add("Foo", "c", () => 123)
                    .Build()
            );

            QueryDocumentReader queryDocumentReader = new QueryDocumentReader();
            QueryDocument queryDocument = queryDocumentReader.Read(
                @"
                {
                    getFoo
                    {
                        x: a
                        y: b
                        z: c
                    }
                }
                "
            );

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, queryDocument, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("getFoo"));
            Assert.IsType<Dictionary<string, object>>(result.Data["getFoo"]);

            Dictionary<string, object> fooObject = (Dictionary<string, object>)result.Data["getFoo"];
            Assert.True(fooObject.ContainsKey("x"));
            Assert.True(fooObject.ContainsKey("y"));
            Assert.True(fooObject.ContainsKey("z"));

            Assert.Equal("hello", fooObject["x"]);
            Assert.Equal("world", fooObject["y"]);
            Assert.Equal(123, fooObject["z"]);
        }

        [Fact]
        public async Task CallGetFooAndResolveWithDynamicObject()
        {
            // arrange
            string expectedResult = Guid.NewGuid().ToString("N");

            Schema schema = Schema.Create(
                @"
                type Foo
                {
                    a: String!
                    b: String
                    c: Int
                }

                type Query {
                    getFoo: Foo 
                }
                ",

                ResolverBuilder.Create()
                    .Add("Query", "getFoo", () => new FooMock())
                    .Build()
            );

            QueryDocumentReader queryDocumentReader = new QueryDocumentReader();
            QueryDocument queryDocument = queryDocumentReader.Read(
                @"
                {
                    getFoo
                    {
                        x: a
                        y: b
                        z: c
                    }
                }
                "
            );

            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, queryDocument, null, null, null, CancellationToken.None);

            // assert
            Assert.Null(result.Errors);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("getFoo"));
            Assert.IsType<Dictionary<string, object>>(result.Data["getFoo"]);

            Dictionary<string, object> fooObject = (Dictionary<string, object>)result.Data["getFoo"];
            Assert.True(fooObject.ContainsKey("x"));
            Assert.True(fooObject.ContainsKey("y"));
            Assert.True(fooObject.ContainsKey("z"));

            Assert.Equal("hello", fooObject["x"]);
            Assert.Equal("world", fooObject["y"]);
            Assert.Equal(123, fooObject["z"]);
        }

    }

    public class FooMock
    {
        public string A { get; } = "hello";
        public string B { get; } = "world";
        public int C { get; } = 123;
    }
}