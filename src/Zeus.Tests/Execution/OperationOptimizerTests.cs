using System.Linq;
using Zeus;
using Zeus.Abstractions;
using Zeus.Execution;
using Zeus.Parser;
using Zeus.Resolvers;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class OperationOptimizerTests
    {
        [Fact]
        public void OptimizeValidQuery()
        {
            // arrange
            ISchema schema = CreateSchema();
            QueryDocument query = CreateSimpleValidQuery();

            // act
            OperationOptimizer operationOptimizer = new OperationOptimizer();
            IOptimizedOperation operation = operationOptimizer.Optimize(schema, query, "getBars");

            // assert
            Assert.Equal(OperationType.Query, operation.Operation.Type);
            Assert.Single(operation.Selections);

            IOptimizedSelection foosField = operation.Selections.First();
            Assert.Equal("bars", foosField.Name);
            Assert.Equal("foos", foosField.FieldDefinition.Name);
            Assert.True(foosField.FieldDefinition.Type.IsListType());

            IOptimizedSelection[] fields = foosField.GetSelections(foosField.FieldDefinition.Type).ToArray();
            Assert.Single(fields);
            Assert.Equal("a", fields.First().Name);
            Assert.Equal("x", fields.First().FieldDefinition.Name);
            Assert.True(fields.First().FieldDefinition.Type.IsScalarType());
        }

        [Fact]
        public void OptimizeValidQueryWithFragments()
        {
            // arrange
            ISchema schema = CreateSchema();
            QueryDocument query = CreateValidQueryWithFragmentSpread();

            // act
            OperationOptimizer operationOptimizer = new OperationOptimizer();
            IOptimizedOperation operation = operationOptimizer.Optimize(schema, query, "getBars");

            // assert
            Assert.Equal(OperationType.Query, operation.Operation.Type);
            Assert.Single(operation.Selections);

            IOptimizedSelection foosField = operation.Selections.First();
            Assert.Equal("bars", foosField.Name);
            Assert.Equal("foos", foosField.FieldDefinition.Name);
            Assert.True(foosField.FieldDefinition.Type.IsListType());

            IOptimizedSelection[] fields = foosField.GetSelections(foosField.FieldDefinition.Type).ToArray();
            Assert.Single(fields);
            Assert.Equal("a", fields.First().Name);
            Assert.Equal("x", fields.First().FieldDefinition.Name);
            Assert.True(fields.First().FieldDefinition.Type.IsScalarType());
        }

        /// <summary>
        /// Optimizes a query that referrs to fields that do not exists.
        /// The query optimizer should skip invalid field and the optimized 
        /// operation structure should only contain valid parts of the query.
        /// </summary>
        [Fact]
        public void OptimizeInvalidQuery()
        {
            // arrange
            ISchema schema = CreateSchema();
            QueryDocument query = CreateSimpleInvalidQuery();

            // act
            OperationOptimizer operationOptimizer = new OperationOptimizer();
            IOptimizedOperation operation = operationOptimizer.Optimize(schema, query, "getBars");

            // assert
            Assert.Equal(OperationType.Query, operation.Operation.Type);
            Assert.Single(operation.Selections);

            IOptimizedSelection foosField = operation.Selections.First();
            Assert.Equal("bars", foosField.Name);
            Assert.Equal("foos", foosField.FieldDefinition.Name);
            Assert.True(foosField.FieldDefinition.Type.IsListType());

            IOptimizedSelection[] fields = foosField.GetSelections(foosField.FieldDefinition.Type).ToArray();
            Assert.Single(fields);
            Assert.Equal("a", fields.First().Name);
            Assert.Equal("x", fields.First().FieldDefinition.Name);
            Assert.True(fields.First().FieldDefinition.Type.IsScalarType());
        }


        private ISchema CreateSchema()
        {
            string schemaDocument = @"
                type Foo {
                    x: String!
                    y: Int
                }

                type Query {
                    foos: [Foo!]
                }
            ";

            IResolverCollection resolvers = ResolverBuilder
                .Create()
                .Add("query", "foos", () => new[] { new object() })
                .Add("Foo", "x", () => "hello world")
                .Add("Foo", "y", () => 123456)
                .Build();

            return Schema.Create(schemaDocument, resolvers);
        }

        private QueryDocument CreateSimpleValidQuery()
        {
            string queryDocument = @"
                query getBars {
                    bars: foos {
                        a: x
                    }
                }
            ";

            QueryDocumentReader documentReader = new QueryDocumentReader();
            return documentReader.Read(queryDocument);
        }

        private QueryDocument CreateSimpleInvalidQuery()
        {
            string queryDocument = @"
                query getBars {
                    bars: foos {
                        a: x
                        p
                    }
                }
            ";

            QueryDocumentReader documentReader = new QueryDocumentReader();
            return documentReader.Read(queryDocument);
        }

        private QueryDocument CreateValidQueryWithFragmentSpread()
        {
            string queryDocument = @"
                query getBars {
                    bars: foos {
                        ...fooFields
                    }
                }

                fragment fooFields on Foo {
                    a: x
                }
            ";

            QueryDocumentReader documentReader = new QueryDocumentReader();
            return documentReader.Read(queryDocument);
        }
    }
}