using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Generators
{
    public class InterfaceGeneratorTests
    {
        [Fact]
        public async Task Create_Interface_From_Object()
        {
            // arrange
            var schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"
                        type Query {
                            abc: String
                            def: Int
                        }
                    ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            INamedType type = schema.QueryType;

            var text = new StringBuilder();
            var writer = new CodeWriter(text);
            var generator = new InterfaceGenerator();

            var list = new List<ISelectionNode>
            {
                new FieldNode(
                    null,
                    new NameNode("abc"),
                    new NameNode("abc_alias"),
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null),
                new FieldNode(
                    null,
                    new NameNode("def"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null),
            };

            var selectionSet = new SelectionSetNode(null, list);

            var typeLookup = new Mock<ITypeLookup>();
            typeLookup.Setup(t => t.GetTypeName(default, default)).Returns("string");

            // act
            await generator.WriteAsync(
                writer,
                schema, type,
                selectionSet,
                list.OfType<FieldNode>(),
                "Query",
                typeLookup.Object);

            // assert
            await writer.FlushAsync();

            text.ToString().MatchSnapshot();
        }
    }

}
