using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ErrorGeneratorTests
    {
        [Fact]
        public void Generate_NoErrors()
        {
            AssertResult(
                FileResource.Open("Query.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("Schema.graphql"));
        }

        [Fact]
        public void Generate_SyntaxError()
        {
            Assert.Collection(
                AssertError(
                    FileResource.Open("Query_SyntaxError.graphql"),
                    FileResource.Open("Schema.extensions.graphql"),
                    FileResource.Open("Schema.graphql")),
                error => { });
        }

        [Fact]
        public void Generate_SchemaValidationError()
        {
            Assert.Collection(
                AssertError(
                    FileResource.Open("Query_SchemaValidationError.graphql"),
                    FileResource.Open("Schema.extensions.graphql"),
                    FileResource.Open("Schema.graphql")),
                error => { });
        }

        [Fact]
        public void Generate_ChatClient_InvalidNullCheck()
        {
            AssertResult(
                FileResource.Open("ChatMeFiendsNodes.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("ChatSchema.graphql"));
        }
    }
}
