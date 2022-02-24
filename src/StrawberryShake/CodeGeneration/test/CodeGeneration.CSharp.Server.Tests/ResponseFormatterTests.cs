using System.IO;
using ChilliCream.Testing;
using Xunit;
using static Xunit.Assert;

namespace StrawberryShake.CodeGeneration.CSharp;

public class ResponseFormatterTests
{
    [Fact]
    public void Format_And_Take_Minimal()
    {
        string? fileName = null;

        try
        {
            // arrange
            fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // act
            var document = new GeneratorDocument(
                "Schema.graphql",
                FileResource.Open("Schema.graphql"),
                GeneratorDocumentKind.GraphQL);

            var error = new GeneratorError("abc", "def", "ghi");

            var response = new GeneratorResponse(new[] {document}, new[] {error});
            ResponseFormatter.Format(response, fileName);
            GeneratorResponse parsed = ResponseFormatter.Take(fileName);

            // assert
            Equal(response.Documents.Count, parsed.Documents.Count);

            for (var i = 0; i < response.Documents.Count; i++)
            {
                Equal(response.Documents[i].Hash, parsed.Documents[i].Hash);
                Equal(response.Documents[i].Kind, parsed.Documents[i].Kind);
                Equal(response.Documents[i].Name, parsed.Documents[i].Name);
                Equal(response.Documents[i].Path, parsed.Documents[i].Path);
                Equal(response.Documents[i].SourceText, parsed.Documents[i].SourceText);
            }

            for (var i = 0; i < response.Errors.Count; i++)
            {
                Equal(response.Errors[i].Code, parsed.Errors[i].Code);
                Equal(response.Errors[i].Location?.Column, parsed.Errors[i].Location?.Column);
                Equal(response.Errors[i].Location?.Line, parsed.Errors[i].Location?.Line);
                Equal(response.Errors[i].Message, parsed.Errors[i].Message);
                Equal(response.Errors[i].Title, parsed.Errors[i].Title);
                Equal(response.Errors[i].FilePath, parsed.Errors[i].FilePath);
            }
        }
        finally
        {
            if (fileName is not null && File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Format_And_Take_All()
    {
        string? fileName = null;

        try
        {
            // arrange
            fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // act
            var document = new GeneratorDocument(
                "Schema.graphql",
                FileResource.Open("Schema.graphql"),
                GeneratorDocumentKind.GraphQL,
                "abc",
                "def/ghi");

            var error = new GeneratorError("abc", "def", "ghi", fileName, new Location(1, 2));

            var response = new GeneratorResponse(new[] {document}, new[] {error});
            ResponseFormatter.Format(response, fileName);
            GeneratorResponse parsed = ResponseFormatter.Take(fileName);

            // assert
            Equal(response.Documents.Count, parsed.Documents.Count);

            for (var i = 0; i < response.Documents.Count; i++)
            {
                Equal(response.Documents[i].Hash, parsed.Documents[i].Hash);
                Equal(response.Documents[i].Kind, parsed.Documents[i].Kind);
                Equal(response.Documents[i].Name, parsed.Documents[i].Name);
                Equal(response.Documents[i].Path, parsed.Documents[i].Path);
                Equal(response.Documents[i].SourceText, parsed.Documents[i].SourceText);
            }

            for (var i = 0; i < response.Errors.Count; i++)
            {
                Equal(response.Errors[i].Code, parsed.Errors[i].Code);
                Equal(response.Errors[i].Location?.Column, parsed.Errors[i].Location?.Column);
                Equal(response.Errors[i].Location?.Line, parsed.Errors[i].Location?.Line);
                Equal(response.Errors[i].Message, parsed.Errors[i].Message);
                Equal(response.Errors[i].Title, parsed.Errors[i].Title);
                Equal(response.Errors[i].FilePath, parsed.Errors[i].FilePath);
            }
        }
        finally
        {
            if (fileName is not null && File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
