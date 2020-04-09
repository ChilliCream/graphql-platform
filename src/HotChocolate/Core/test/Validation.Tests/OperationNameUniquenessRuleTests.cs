using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class OperationNameUniquenessRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public OperationNameUniquenessRuleTests()
            : base(builder => builder.AddOperationRules())
        {
        }

        [Fact]
        public void TwoUniqueQueryOperations()
        {
            ExpectValid(@"
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
        }


        [Fact]
        public void TwoQueryOperationsWithTheSameName()
        {
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(
                $"The operation name `getName` is not unique.",
                t.Message));
        }

        [Fact]
        public void TwoOperationsWithTheSameName()
        {
            ExpectErrors(@"
                query dogOperation {
                    dog {
                        name
                    }
                }

                mutation dogOperation {
                    mutateDog {
                        id
                    }
                }
            ",
            t => Assert.Equal(
                $"The operation name `dogOperation` is not unique.",
                t.Message));
        }
    }
}
