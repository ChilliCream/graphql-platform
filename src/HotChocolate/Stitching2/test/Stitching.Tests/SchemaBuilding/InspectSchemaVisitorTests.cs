using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using ChilliCream.Testing;
using Xunit;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class InspectSchemaVisitorTests
    {
        [Fact]
        public void Test()
        {
            // arrange
            var schema = Utf8GraphQLParser.Parse(
                @"schema @rename(from: ""Foo"", to: ""Bar"") { }
                type Foo { field: String }");
            var context = new SchemaRewriterContext { Rewriters = { new RenameSchemaRewriter() } };

            // act
            var visitor = new SchemaInspector();
            visitor.Visit(schema, context);

            // assert
            var rewriter = new SchemaRewriter();
            rewriter.Rewrite(schema, context).Print().MatchSnapshot();
        }

    }
}
