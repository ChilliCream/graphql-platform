﻿using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate
{
    public class SchemaFirstTests
    {
        [Fact]
        public async Task ExecuteOneFieldQuery()
        {
            // arrange
            Schema schema = Schema.Create(
                @"
                type Query {
                    test: String
                }",
                c => c.BindType<Query>());

            // act
            IExecutionResult result = await schema.ExecuteAsync("{ test }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }
    }

    public class Query
    {
        public string GetTest()
        {
            return "Hello World!";
        }

        public string TestProp => "Hello World!";
    }
}
