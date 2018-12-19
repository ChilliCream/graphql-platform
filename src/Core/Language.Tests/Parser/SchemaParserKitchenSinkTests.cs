using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaParserKitchenSinkTests
    {
        [Fact]
        public void ParseFacebookKitchenSinkSchema()
        {
            // arrange
            string schemaSource = FileResource.Open("schema-kitchen-sink.graphql");

            // act
            Parser parser = new Parser();
            DocumentNode document = parser.Parse(
                schemaSource, new ParserOptions(noLocations: true));

            // assert
            document.Snapshot();
        }

        [Fact]
        public void ParseFacebookKitchenSinkQuery()
        {
            // arrange
            string querySource = FileResource.Open("kitchen-sink.graphql");

            // act
            Parser parser = new Parser();
            DocumentNode document = parser.Parse(
                querySource, new ParserOptions(
                    noLocations: true, allowFragmentVariables: true));

            // assert
            document.Snapshot();
        }
    }
}
