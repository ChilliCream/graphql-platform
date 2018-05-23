using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task ExecuteGraphiQLIntrospectionQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            DocumentNode query = CreateGraphiQLIntrospectionQuery();

            // act
            OperationExecuter operationExecuter = new OperationExecuter();
            Stopwatch sw = Stopwatch.StartNew();
            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, query, null, null, null, CancellationToken.None);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
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
