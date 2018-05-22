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
using Newtonsoft.Json;
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
            Assert.Equal(
                "{\"Data\":{\"__type\":{\"name\":\"Foo\"}},\"Errors\":null}",
                JsonConvert.SerializeObject(result));
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
            Assert.Equal(
                "{\"Data\":{\"__type\":{\"name\":\"Foo\"," +
                "\"fields\":[{\"name\":\"a\",\"type\":{\"name\":\"String\"}}]}}," +
                "\"Errors\":null}",
                JsonConvert.SerializeObject(result));
        }

        [Fact]
        public async Task ExecuteGraphiQLIntrospectionQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = CreateGraphiQLIntrospectionQuery();

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            string s = JsonConvert.SerializeObject(result);
            Assert.Equal(
                "{\"Data\":{\"__type\":{\"name\":\"Foo\"," +
                "\"fields\":[{\"name\":\"a\",\"type\":{\"name\":\"String\"}}]}}," +
                "\"Errors\":null}",
                JsonConvert.SerializeObject(result));
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

        private DocumentNode CreateGraphiQLIntrospectionQuery()
        {
            return Parser.Default.Parse(@"
                query IntrospectionQuery {
                __schema {
                    queryType { name }
                    mutationType { name }
                    subscriptionType { name }
                    types {
                    ...FullType
                    }
                    directives {
                    name
                    description
                    locations
                    args {
                        ...InputValue
                    }
                    }
                }
                }

                fragment FullType on __Type {
                kind
                name
                description
                fields(includeDeprecated: true) {
                    name
                    description
                    args {
                    ...InputValue
                    }
                    type {
                    ...TypeRef
                    }
                    isDeprecated
                    deprecationReason
                }
                inputFields {
                    ...InputValue
                }
                interfaces {
                    ...TypeRef
                }
                enumValues(includeDeprecated: true) {
                    name
                    description
                    isDeprecated
                    deprecationReason
                }
                possibleTypes {
                    ...TypeRef
                }
                }

                fragment InputValue on __InputValue {
                name
                description
                type { ...TypeRef }
                defaultValue
                }

                fragment InputValue on __ObjectType {
                name
                description
                type { ...TypeRef }
                defaultValue
                Bla
                }

                fragment TypeRef on __Type {
                kind
                name
                ofType {
                    kind
                    name
                    ofType {
                    kind
                    name
                    ofType {
                        kind
                        name
                        ofType {
                        kind
                        name
                        ofType {
                            kind
                            name
                            ofType {
                            kind
                            name
                            ofType {
                                kind
                                name
                            }
                            }
                        }
                        }
                    }
                    }
                }
                }
            ");
        }
    }
}
