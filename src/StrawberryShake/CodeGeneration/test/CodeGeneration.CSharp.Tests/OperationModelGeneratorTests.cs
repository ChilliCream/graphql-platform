using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationModelGeneratorTests
    {
        [Fact]
        public async Task GenerateClient_With_Singel_Optional_Argument()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new OperationModelGenerator();

            var descriptor = new OperationModelDescriptor(
                "OnReviewOperation",
                "Demo",
                "onReview",
                "global::Demo.IOnReview",
                "Queries",
                "Subscription",
                new List<OperationArgumentDescriptor>
                {
                    new OperationArgumentDescriptor(
                        "Episode",
                        "episode",
                        "episode",
                        "global::StrawberryShake.Optional<global::Demo.Episode>",
                        "Episode",
                        true)
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateClient_With_No_Argument()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new OperationModelGenerator();

            var descriptor = new OperationModelDescriptor(
                "OnReviewOperation",
                "Demo",
                "onReview",
                "global::Demo.IOnReview",
                "Queries",
                "Subscription",
                new List<OperationArgumentDescriptor>());

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task GenerateClient_With_Multiple_Arguments()
        {
            // arrange
            var sb = new StringBuilder();
            var writer = new CodeWriter(sb);

            var generator = new OperationModelGenerator();

            var descriptor = new OperationModelDescriptor(
                "OnReviewOperation",
                "Demo",
                "onReview",
                "global::Demo.IOnReview",
                "Queries",
                "Subscription",
                new List<OperationArgumentDescriptor>
                {
                    new OperationArgumentDescriptor(
                        "Episode",
                        "episode",
                        "episode",
                        "global::StrawberryShake.Optional<global::Demo.Episode>",
                        "Episode",
                        true),
                    new OperationArgumentDescriptor(
                        "Abc",
                        "abcd",
                        "abc",
                        "string",
                        "String",
                        false),
                    new OperationArgumentDescriptor(
                        "Def",
                        "defg",
                        "def",
                        "int",
                        "Int",
                        false)
                });

            // act
            await generator.WriteAsync(writer, descriptor);

            // assert
            sb.ToString().MatchSnapshot();
        }

        [Fact]
        public void CanHandle()
        {
            // arrange
            var generator = new OperationModelGenerator();

            var descriptor = new OperationModelDescriptor(
                "OnReviewOperation",
                "Demo",
                "onReview",
                "global::Demo.IOnReview",
                "Queries",
                "Subscription",
                new List<OperationArgumentDescriptor>());

            // act
            var canHandle = generator.CanHandle(descriptor);

            // assert
            Assert.True(canHandle);
        }
    }
}
