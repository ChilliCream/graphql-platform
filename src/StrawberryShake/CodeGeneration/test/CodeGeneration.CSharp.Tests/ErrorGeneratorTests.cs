using ChilliCream.Testing;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

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
                Path.Combine("__resources__", "Query_SyntaxError.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "Schema.graphql")),
            error =>
            {
                Assert.Equal("SS0001", error.Code);
                Assert.Equal(
                    "Expected a `RightBrace`-token, but found a `EndOfFile`-token.",
                    error.Message);
            });
    }

    [Fact]
    public void Generate_SchemaValidationError()
    {
        Assert.Collection(
            AssertError(
                Path.Combine("__resources__", "Query_SchemaValidationError.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "Schema.graphql")),
            error =>
            {
                Assert.Equal("SS0002", error.Code);
                Assert.Equal(
                    "The field `someNotExistingField` does not exist on the type `Character`.",
                    error.Message);
            });
    }

    [Fact]
    public void Generate_ChatClient_InvalidNullCheck()
    {
        AssertResult(
            FileResource.Open("ChatMeFiendsNodes.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("ChatSchema.graphql"));
    }

    // TODO : this is a bug with the code generation.
    /*
    [Fact]
    public void ClosePaymentsMutationErrors()
    {
        AssertResult(
            FileResource.Open("PaymentSchema.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("PaymentQuery.graphql"));
    }
    */
}
