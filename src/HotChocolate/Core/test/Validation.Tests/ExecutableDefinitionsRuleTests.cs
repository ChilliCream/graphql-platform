using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class ExecutableDefinitionsRuleTests
    : DocumentValidatorVisitorTestBase
{
    public ExecutableDefinitionsRuleTests()
        : base(builder => builder.AddDocumentRules())
    {
    }

    [Fact]
    public void QueryWithTypeSystemDefinitions()
    {
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
