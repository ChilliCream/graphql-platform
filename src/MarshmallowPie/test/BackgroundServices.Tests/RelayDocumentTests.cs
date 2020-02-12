using System;
using ChilliCream.Testing;
using HotChocolate.Language;
using MarshmallowPie.Processing;
using Snapshooter.Xunit;
using Xunit;

namespace MarshmallowPie.BackgroundServices
{
    public class RelayDocumentTests
    {
        [Fact]
        public void Parse_Valid_Document_No_Hash_Details()
        {
            // arrange
            string sourceText = FileResource.Open("relay.json");
            var documentInfo = new DocumentInfo("abc", null, null, null);

            // act
            RelayDocument document = RelayDocument.Parse(documentInfo, sourceText);

            // assert
            document.MatchSnapshot();
        }

        [Fact]
        public void Parse_Valid_Document_With_Hash_Details()
        {
            // arrange
            string sourceText = FileResource.Open("relay.json");
            var documentInfo = new DocumentInfo("abc", null, "sha1", HashFormat.Base64);

            // act
            RelayDocument document = RelayDocument.Parse(documentInfo, sourceText);

            // assert
            document.MatchSnapshot();
        }

        [Fact]
        public void Parse_DocumentInfo_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => RelayDocument.Parse(null, "abc"));
        }

        [Fact]
        public void Parse_SourceText_Is_Null()
        {
            Assert.Throws<ArgumentException>(
                () => RelayDocument.Parse(
                    new DocumentInfo("abc", null, "sha1", HashFormat.Base64),
                    null));
        }

        [Fact]
        public void Parse_SourceText_Is_Empty()
        {
            Assert.Throws<ArgumentException>(
                () => RelayDocument.Parse(
                    new DocumentInfo("abc", null, "sha1", HashFormat.Base64),
                    string.Empty));
        }
    }
}
