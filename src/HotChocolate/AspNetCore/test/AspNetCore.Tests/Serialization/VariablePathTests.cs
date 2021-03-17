using Xunit;

namespace HotChocolate.AspNetCore.Serialization
{
    public class VariablePathTests
    {
        [Fact]
        public void PathHasNoFields()
        {
            // arrange
            const string path = "variables";

            // act
            void Parse() => VariablePath.Parse(path);

            // assert
            Assert.Throws<GraphQLException>(Parse);
        }
    }
}
