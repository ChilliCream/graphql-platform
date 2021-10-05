using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class InspectSchemaVisitorTests
    {
        [Fact]
        public void Test()
        {
            // arrange
            var schema = Utf8GraphQLParser.Parse("schema @rename(from: \"Foo\" to: \"Bar\") { }");
            var context = new SchemaRewriterContext { Rewriters = { new RenameSchemaRewriter() } };

            // act
            var visitor = new InspectSchemaVisitor();
            visitor.Visit(schema, context);
        }

    }
}