using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.Execution;

public class OperationDocumentTests
{
    [Fact]
    public void Create_Document_IsNull()
    {
        // arrange
        // act
        void Action() => new OperationDocument(null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Create_Document()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("{ a }");

        // act
        var query = new OperationDocument(document);

        // assert
        Assert.Equal(document, query.Document);
    }

    [Fact]
    public void QueryDocument_ToString()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("{ a }");

        // act
        var query = new OperationDocument(document);

        // assert
        query.Document.ToString(false).MatchSnapshot();
    }

    [Fact]
    public void QueryDocument_ToSource()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("{ a }");

        // act
        var query = new OperationDocument(document);

        // assert
        Utf8GraphQLParser
            .Parse(query.AsSpan())
            .Print(true)
            .MatchSnapshot();
    }

    [Fact]
    public async Task QueryDocument_WriteToAsync()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("{ a }");
        var query = new OperationDocument(document);
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
            .Print(true)
            .MatchSnapshot();
    }
}