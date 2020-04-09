using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class LoneAnonymousOperationRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public LoneAnonymousOperationRuleTests()
            : base(builder => builder.AddOperationRules())
        {
        }

        [Fact]
        public void QueryContainsOneAnonymousOperation()
        {
            ExpectValid(@"
                {
                    dog {
                        name
                    }
                }
            ");
        }

        [Fact]
        public void QueryWithOneAnonymousAndOneNamedOperation()
        {
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(
                "GraphQL allows a short‐hand form for defining query " +
                "operations when only that one operation exists in the " +
                "document.", t.Message));
        }

        [Fact]
        public void QueryWithTwoAnonymousOperations()
        {
            ExpectErrors(@"
                {
                    dog {
                        name
                    }
                }

                {
                    dog {
                        name
                    }
                }
            ",
            t => Assert.Equal(
                "GraphQL allows a short‐hand form for defining query " +
                "operations when only that one operation exists in the " +
                "document.", t.Message));
        }
    }
}
