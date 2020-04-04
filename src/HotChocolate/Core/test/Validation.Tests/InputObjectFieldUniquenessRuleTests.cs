using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class InputObjectFieldUniquenessRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public InputObjectFieldUniquenessRuleTests()
            : base(services => services.AddInputObjectsAreValidRule())
        {
        }

        [Fact]
        public void NoFieldAmbiguity()
        {
            ExpectValid(@"
                {
                    findDog(complex: { name: ""A"", owner: ""B"" })
                }
            ");
        }

        [Fact]
        public void NameFieldIsAmbiguous()
        {
            // arrange
            ExpectErrors(@"
                {
                    findDog(complex: { name: ""A"", name: ""B"" })
                }
            ",
            error => Assert.Equal("Field `name` is ambiguous.", error.Message));
        }
    }
}
