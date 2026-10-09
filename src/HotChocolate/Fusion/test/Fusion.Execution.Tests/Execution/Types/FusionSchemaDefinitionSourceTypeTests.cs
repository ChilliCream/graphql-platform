using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionSourceTypeTests
{
    [Fact]
    public void Create_Should_KeepUnionMembersAsObjects_When_QueryPrecedesUnion()
    {
        var schema = CreateSchema(
            """
            type Query @fusion__type(schema: A) {
              book: Media @fusion__field(schema: A, sourceType: "Book")
            }

            type Book @fusion__type(schema: A) {
              id: ID!
            }

            union Media
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Book") = Book
            """);

        DescribeField(schema, "book", "Media").MatchInlineSnapshot(
            """
            Composite field type: Media
            Source field type: Media
            Source type name: Book
            Union members: Book
            """);
    }

    [Fact]
    public void Create_Should_KeepCompositeSourceType_When_UnionPrecedesQuery()
    {
        var schema = CreateSchema(
            """
            type Book @fusion__type(schema: A) {
              id: ID!
            }

            union Media
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Book") = Book

            type Query @fusion__type(schema: A) {
              book: Media @fusion__field(schema: A, sourceType: "Book")
            }
            """);

        DescribeField(schema, "book", "Media").MatchInlineSnapshot(
            """
            Composite field type: Media
            Source field type: Media
            Source type name: Book
            Union members: Book
            """);
    }

    [Fact]
    public void Create_Should_ResolveSourceSyntaxAgainstEachCompositeType_When_SyntaxIsShared()
    {
        var schema = CreateSchema(
            """
            type Query @fusion__type(schema: A) {
              media: [Media!]! @fusion__field(schema: A, sourceType: "[Book!]!")
              viewerMedia: [ViewerMedia!]!
                @fusion__field(schema: A, sourceType: "[Book!]!")
            }

            type Book @fusion__type(schema: A) {
              id: ID!
            }

            union Media
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Book") = Book

            union ViewerMedia
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Book") = Book
            """);

        var query = schema.Types.GetType<FusionObjectTypeDefinition>("Query");
        var mediaSource = query.Fields["media"].Sources["A"];
        var viewerMediaSource = query.Fields["viewerMedia"].Sources["A"];

        $"""
        Media source field type: {FormatType(mediaSource.Type)}
        Media source type name: {mediaSource.SourceTypeName}
        ViewerMedia source field type: {FormatType(viewerMediaSource.Type)}
        ViewerMedia source type name: {viewerMediaSource.SourceTypeName}
        """.MatchInlineSnapshot(
            """
            Media source field type: [Media!]!
            Media source type name: Book
            ViewerMedia source field type: [ViewerMedia!]!
            ViewerMedia source type name: Book
            """);
    }

    [Fact]
    public void Create_Should_PreserveSourceTypeWrappers_When_NamedTypeDiffers()
    {
        var schema = CreateSchema(
            """
            type Query @fusion__type(schema: A) {
              books: [Media!]!
                @fusion__field(schema: A, sourceType: "[Book!]!")
            }

            type Book @fusion__type(schema: A) {
              id: ID!
            }

            union Media
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Book") = Book
            """);

        DescribeField(schema, "books", "Media").MatchInlineSnapshot(
            """
            Composite field type: [Media!]!
            Source field type: [Media!]!
            Source type name: Book
            Union members: Book
            """);
    }

    [Fact]
    public void Create_Should_ReuseCachedType_When_SourceAndCompositeTypeMatch()
    {
        var schema = CreateSchema(
            """
            type Query @fusion__type(schema: A) {
              media: Media @fusion__field(schema: A)
            }

            type Book @fusion__type(schema: A) {
              id: ID!
            }

            union Media
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Book") = Book
            """);

        var field = schema.QueryType.Fields["media"];
        var source = field.Sources["A"];

        $"""
        {DescribeField(schema, "media", "Media")}
        Composite and source type are same instance: {ReferenceEquals(field.Type, source.Type)}
        """.MatchInlineSnapshot(
            """
            Composite field type: Media
            Source field type: Media
            Source type name:
            Union members: Book
            Composite and source type are same instance: True
            """);
    }

    private static FusionSchemaDefinition CreateSchema(string schema)
        => FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(
                schema
                + """

                  enum fusion__Schema {
                    A
                  }
                  """));

    private static string DescribeField(
        FusionSchemaDefinition schema,
        string fieldName,
        string unionName)
    {
        var field = schema.QueryType.Fields[fieldName];
        var source = field.Sources["A"];
        var union = schema.Types.GetType<FusionUnionTypeDefinition>(unionName);

        return $"""
            Composite field type: {FormatType(field.Type)}
            Source field type: {FormatType(source.Type)}
            Source type name: {source.SourceTypeName}
            Union members: {string.Join(", ", union.Types.AsEnumerable().Select(type => type.Name))}
            """;
    }

    private static string FormatType(IType type)
        => type switch
        {
            NonNullType nonNullType => $"{FormatType(nonNullType.NullableType)}!",
            ListType listType => $"[{FormatType(listType.ElementType)}]",
            ITypeDefinition typeDefinition => typeDefinition.Name,
            _ => throw new InvalidOperationException()
        };
}
