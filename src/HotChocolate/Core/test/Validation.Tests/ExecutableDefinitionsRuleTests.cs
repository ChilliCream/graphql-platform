using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class ExecutableDefinitionsRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public ExecutableDefinitionsRuleTests()
            : base(services => services.AddDocumentRules())
        {
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            ExpectErrors(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }

                extend type Dog {
                    color: String
                }
            ",
            t => Assert.Equal(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.", t.Message));
        }

        [Fact]
        public void QueryWithoutTypeSystemDefinitions()
        {
            // arrange
            ExpectValid(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }
            ");
        }

        [Fact]
        public void GoodExecuableDefinitionsWithOnlyOperation()
        {
            // arrange
            ExpectValid(@"
                 query Foo {
                    dog {
                        name
                    }
                }
            ");
        }

        [Fact]
        public void GoodExecuableDefinitionsWithOperationAndFragment()
        {
            // arrange
            ExpectValid(@"
                query Foo {
                    dog {
                        name
                        ...Frag
                    }
                }
                fragment Frag on Dog {
                    name
                }
            ");
        }

        [Fact]
        public void GoodExecuableDefinitionsWithTypeDefinitions()
        {
            // arrange
            ExpectErrors(@"
                query Foo {
                    dog {
                        name
                    }
                }
                type Cow {
                    name: String
                }
                extend type Dog {
                    color: String
                }
            ");
        }

        [Fact]
        public void GoodExecuableDefinitionsWithSchemaDefinitions()
        {
            // arrange
            ExpectErrors(@"
                schema {
                    query: Query
                }

                type Query {
                    test: String
                }

                extend schema @directive
            ");
        }
    }
}
