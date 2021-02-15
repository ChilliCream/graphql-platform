using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
            AssertResult(result, false);
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
            AssertResult(result, false);
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
        public void Generate_BookClient_DataOnly_UnionDataTypes()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "BookUnionQuery.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "BookSchema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        [Fact]
        public void Generate_BookClient_DataOnly_InterfaceDataTypes()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "BookInterfaceQuery.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "BookSchema.graphql")
            };

            // act
            var generator = new CSharpGenerator();
            var result = generator.Generate(fileNames);

            // assert
            AssertResult(result);
        }

        [Fact]
        public void Generate_BookClient_DataInEntity_UnionDataTypes()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "BookUnionQueryWithEntity.graphql"),
                Path.Combine("__resources__", "Schema.extensions.graphql"),
                Path.Combine("__resources__", "BookSchema.graphql")
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

        [Fact]
        public void Generate_ChatClient_AllOperations()
        {
            // arrange
            string[] fileNames =
            {
                Path.Combine("__resources__", "ChatOperations.graphql"),
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
            CSharpGeneratorResult result,
            bool evaluateDiagnostics = true)
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

            var documentName = new HashSet<string>();
            foreach (var document in result.CSharpDocuments)
            {
                if (!documentName.Add(document.Name))
                {
                    Assert.True(false, $"Document name duplicated {document.Name}");
                }

                content.AppendLine("// " + document.Name);
                content.AppendLine();
                content.AppendLine(document.SourceText);
                content.AppendLine();
            }

            content.ToString().MatchSnapshot();

            if (!evaluateDiagnostics)
            {
                return;
            }

            IReadOnlyList<Diagnostic> diagnostics =
                CSharpCompiler.GetDiagnosticErrors(content.ToString());

            if (diagnostics.Any())
            {
                Assert.True(false,
                    "Diagnostic Errors: \n" +
                    diagnostics
                        .Select(x =>
                            $"{x.GetMessage()} " +
                            $"(Line: {x.Location.GetLineSpan().StartLinePosition.Line})")
                        .Aggregate((acc, val) => acc + "\n" + val));
            }
        }
    }
}
