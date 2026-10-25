using System.Text;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Serialization;

public class SchemaDocumentFormatterTests
{
    [Fact]
    public void Format_Without_Formatter_Feature_Returns_Document_Unchanged()
    {
        // arrange
        const string sdl =
            """
            type Query {
              hello: String!
            }
            """;
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              hello: String!
            }
            """);
    }

    [Fact]
    public void Format_With_Formatter_Feature_Invokes_Formatter()
    {
        // arrange
        const string sdl =
            """
            type Query {
              hello: String!
            }
            """;
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        schema.Features.Set<ISchemaDocumentFormatter>(new AppendScalarFormatter("Scalar1"));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              hello: String!
            }

            scalar Scalar1
            """);
    }

    [Fact]
    public void Format_Formatter_Receives_Schema()
    {
        // arrange
        const string sdl =
            """
            type Query {
              hello: String!
            }
            """;
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        ISchemaDefinition? receivedSchema = null;
        schema.Features.Set<ISchemaDocumentFormatter>(
            new DelegateFormatter((s, d) =>
            {
                receivedSchema = s;
                return d;
            }));

        // act
        SchemaFormatter.FormatAsString(schema);

        // assert
        Assert.Same(schema, receivedSchema);
    }

    private sealed class AppendScalarFormatter(string scalarName) : ISchemaDocumentFormatter
    {
        public DocumentNode Format(ISchemaDefinition schema, DocumentNode schemaDocument)
        {
            var definitions = schemaDocument.Definitions.ToList();
            definitions.Add(
                new ScalarTypeDefinitionNode(
                    null,
                    new NameNode(scalarName),
                    null,
                    []));
            return new DocumentNode(null, definitions);
        }
    }

    private sealed class DelegateFormatter(Func<ISchemaDefinition, DocumentNode, DocumentNode> format)
        : ISchemaDocumentFormatter
    {
        public DocumentNode Format(ISchemaDefinition schema, DocumentNode schemaDocument)
            => format(schema, schemaDocument);
    }
}
