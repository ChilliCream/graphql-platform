using System.IO;
using System.Linq;
using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorTests
    {
        [Fact]
        public void Generate_NoErrors()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "Query.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "Schema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        [Fact]
        public void Generate_SyntaxError()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "Query_SyntaxError.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "Schema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        [Fact]
        public void Generate_SchemaValidationError()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "Query_SchemaValidationError.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "Schema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        [Fact]
        public void Generate_ChatClient_ConnectionNotAnEntity()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "ChatPeopleNodes.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "ChatSchema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        [Fact]
        public void Generate_ChatClient_MapperMapsEntityOnRootCorrectly()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "ChatSendMessage.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "ChatSchema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        [Fact]
        public void Generate_ChatClient_InvalidNullCheck()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "ChatMeFiendsNodes.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "ChatSchema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        private static void AssertResult(
            CSharpGeneratorResult result)
        {
            var content = new StringBuilder();

            if (result.Errors.Any())
            {
                content.AppendLine("// Errors:");

                foreach (var error in result.Errors)
                {
                    content.AppendLine(error.Message);
                    content.AppendLine();
                }
            }

            content.AppendLine("// Code:");

            foreach (var document in result.CSharpDocuments)
            {
                content.AppendLine("// " + document.Name);
                content.AppendLine();
                content.AppendLine(document.SourceText);
                content.AppendLine();
            }

            content.ToString().MatchSnapshot();
        }
    }
}
