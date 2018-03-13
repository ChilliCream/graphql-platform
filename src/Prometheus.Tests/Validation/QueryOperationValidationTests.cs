using System.Linq;
using Prometheus.Abstractions;
using Prometheus.Parser;
using Xunit;

namespace Prometheus.Validation
{
    public class QueryOperationValidationTests
    {
        [Fact]
        public void OperationNameUniquenessRule_NameIsUnique_Success()
        {
            // arrange
            OperationNameUniquenessRule rule = new OperationNameUniquenessRule();
            ISchemaDocument schema = CreateSchema();
            IQueryDocument query = ParseQuery(@"
                query getDogName {
                    dog {
                        name
                    }
                }

                query getOwnerName {
                    dog {
                        owner {
                        name
                        }
                    }
                }
            ");

            // act
            IValidationResult[] results = rule.Apply(schema, query).ToArray();

            // assert
            Assert.Collection(results,
                t => Assert.IsType<SuccessResult>(t));
        }

        [Fact]
        public void OperationNameUniquenessRule_NameIsNotUnique_SameOperationType_Error()
        {
            // arrange
            OperationNameUniquenessRule rule = new OperationNameUniquenessRule();
            ISchemaDocument schema = CreateSchema();
            IQueryDocument query = ParseQuery(@"
                query getName {
                    dog {
                        name
                    }
                }

                query getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ");

            // act
            IValidationResult[] results = rule.Apply(schema, query).ToArray();

            // assert
            Assert.Collection(results,
                t =>
                {
                    Assert.IsType<ErrorResult>(t);
                    Assert.Equal($"The operation name getName is not unique.",
                        ((ErrorResult)t).Message);
                });
        }

        [Fact]
        public void OperationNameUniquenessRule_NameIsNotUnique_DifferentOperationType_Error()
        {
            // arrange
            OperationNameUniquenessRule rule = new OperationNameUniquenessRule();
            ISchemaDocument schema = CreateSchema();
            IQueryDocument query = ParseQuery(@"
                query getName {
                    dog {
                        name
                    }
                }

                mutation getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ");

            // act
            IValidationResult[] results = rule.Apply(schema, query).ToArray();

            // assert
            Assert.Collection(results,
                t =>
                {
                    Assert.IsType<ErrorResult>(t);
                    Assert.Equal($"The operation name getName is not unique.",
                        ((ErrorResult)t).Message);
                });
        }

        [Fact]
        public void LoneAnonymousOperationRule_SingleAnonymousOperation_Success()
        {
            // arrange
            LoneAnonymousOperationRule rule = new LoneAnonymousOperationRule();
            ISchemaDocument schema = CreateSchema();
            IQueryDocument query = ParseQuery(@"
                {
                    dog {
                        name
                    }
                }
            ");

            // act
            IValidationResult[] results = rule.Apply(schema, query).ToArray();

            // assert
            Assert.Collection(results,
                t => Assert.IsType<SuccessResult>(t));
        }

        [Fact]
        public void LoneAnonymousOperationRule_AnonymousAndNamedOperation_Error()
        {
            // arrange
            LoneAnonymousOperationRule rule = new LoneAnonymousOperationRule();
            ISchemaDocument schema = CreateSchema();
            IQueryDocument query = ParseQuery(@"
                {
                    dog {
                        name
                    }
                }

                query getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ");

            // act
            IValidationResult[] results = rule.Apply(schema, query).ToArray();

            // assert
            Assert.Collection(results,
                t =>
                {
                    Assert.IsType<ErrorResult>(t);
                    Assert.Equal("There is at least one "
                        + " anonymous operation although the query consists of "
                        + "more than one operation.",
                        ((ErrorResult)t).Message);
                });
        }

        private ISchemaDocument CreateSchema()
        {
            SchemaDocumentReader schemaReader = new SchemaDocumentReader();
            return schemaReader.Read(Schemas.Default);
        }

        private IQueryDocument ParseQuery(string query)
        {
            QueryDocumentReader queryReader = new QueryDocumentReader();
            return queryReader.Read(query);
        }
    }
}