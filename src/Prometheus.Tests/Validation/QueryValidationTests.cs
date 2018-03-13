using System.Linq;
using Prometheus.Abstractions;
using Prometheus.Parser;
using Xunit;

namespace Prometheus.Validation
{
    public class QueryValidationTests
    {
        [Fact]
        public void OperationNameIsNotUnique_NameIsUnique_Success()
        {
            // arrange
            OperationNameIsNotUnique rule = new OperationNameIsNotUnique();
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
        public void OperationNameIsNotUnique_NameIsNotUnique_SameOperationType_Error()
        {
            // arrange
            OperationNameIsNotUnique rule = new OperationNameIsNotUnique();
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
        public void OperationNameIsNotUnique_NameIsNotUnique_DifferentOperationType_Error()
        {
            // arrange
            OperationNameIsNotUnique rule = new OperationNameIsNotUnique();
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