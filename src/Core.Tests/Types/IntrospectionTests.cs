using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Factories;
using Xunit;

namespace HotChocolate.Types.Factories
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
            Assert.True(result.Data.ContainsKey("__typename"));
            Assert.Equal("Query", result.Data["__typename"]);
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
            Assert.True(result.Data.ContainsKey("b"));
            Assert.IsType<Dictionary<string, object>>(result.Data["b"]);

            Dictionary<string, object> map = (Dictionary<string, object>)result.Data["b"];
            Assert.True(map.ContainsKey("__typename"));
            Assert.Equal("Foo", map["__typename"]);
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(cnf =>
            {
                cnf.RegisterType(c => new ObjectTypeConfig
                {
                    Name = "Query",
                    Fields = new[]
                    {
                        new Field(new FieldConfig
                        {
                            Name = "a",
                            Type = c.StringType
                        }),
                        new Field(new FieldConfig
                        {
                            Name = "b",
                            Type = () => c.GetOutputType("Foo"),
                            Resolver = () => (ctx, ct) => new object()
                        })
                    }
                });

                cnf.RegisterType(c => new ObjectTypeConfig
                {
                    Name = "Foo",
                    Fields = new[]
                    {
                        new Field(new FieldConfig
                        {
                            Name = "a",
                            Type = c.StringType
                        }),
                    }
                });
            });
        }
    }
}
