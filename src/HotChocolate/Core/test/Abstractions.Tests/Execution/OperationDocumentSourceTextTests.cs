using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.Execution;

public class OperationDocumentSourceTextTests
{
    [Fact]
    public void Create_Document_IsNull()
    {
        // arrange
        // act
        void Action() => new OperationDocumentSourceText(null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Create_Document()
    {
        // arrange
        // act
        var query = new OperationDocumentSourceText("{ a }");

        // assert
        Assert.Equal("{ a }", query.SourceText);
    }

    [Fact]
    public void QueryDocument_ToString()
    {
        // arrange
        // act
        var query = new OperationDocumentSourceText("{ a }");

        // assert
        query.ToString().MatchSnapshot();
    }

    [Fact]
    public void QueryDocument_ToSource()
    {
        // arrange
        // act
        var query = new OperationDocumentSourceText("{ a }");

        // assert
        Utf8GraphQLParser
            .Parse(query.AsSpan())
            .Print(true)
            .MatchSnapshot();
    }

    [Fact]
    public async Task QuerySourceText_WriteToAsync()
    {
        // arrange
        var query = new OperationDocumentSourceText("{ a }");
        byte[] buffer;

        // act
        using (var stream = new MemoryStream())
        {
            await query.WriteToAsync(stream);
            buffer = stream.ToArray();
        }

        // assert
        Utf8GraphQLParser
            .Parse(buffer)
            .Print()
            .MatchSnapshot();
    }
}