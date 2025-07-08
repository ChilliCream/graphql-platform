using HotChocolate.Fusion.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Rewriters;

public sealed class SelectedValueToSelectionSetRewriterTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string typeName, string selectedValue, string selectionSet)
    {
        // arrange
        var selectedValueNode = new FieldSelectionMapParser(selectedValue).Parse();

        // act
        var selectionSetNode =
            s_selectedValueToSelectionSetRewriter.SelectedValueToSelectionSet(
                selectedValueNode,
                s_schema.Types[typeName]);

        // assert
        selectionSetNode.Print().MatchInlineSnapshot(selectionSet);
    }

    public static TheoryData<string, string, string> ExamplesData()
    {
        return new TheoryData<string, string, string>
        {
            {
                "Book",
                "id",
                """
                {
                    id
                }
                """
            },
            {
                "Book",
                "author.id",
                """
                {
                    author {
                        id
                    }
                }
                """
            },
            {
                "Query",
                "mediaById<Book>.isbn",
                """
                {
                    mediaById {
                        ... on Book {
                            isbn
                        }
                    }
                }
                """
            },
            {
                "Product",
                "dimensions.{ size, weight }",
                """
                {
                    dimensions {
                        size
                        weight
                    }
                }
                """
            },
            {
                "Product",
                "{ size: dimensions.size, weight: dimensions.weight }",
                """
                {
                    dimensions {
                        size
                        weight
                    }
                }
                """
            },
            {
                "Product",
                "parts[id]",
                """
                {
                    parts {
                        id
                    }
                }
                """
            },
            {
                "Product",
                "parts[{ id, name }]",
                """
                {
                    parts {
                        id
                        name
                    }
                }
                """
            },
            {
                "Product",
                "parts[[{ id, name }]]",
                """
                {
                    parts {
                        id
                        name
                    }
                }
                """
            },
            {
                "Location",
                "{ coordinates: coordinates[{ lat: x, lon: y }] }",
                """
                {
                    coordinates {
                        x
                        y
                    }
                }
                """
            },
            {
                "Query",
                "mediaById<Book>.title | mediaById<Movie>.movieTitle",
                """
                {
                    mediaById {
                        ... on Book {
                            title
                        }
                        ... on Movie {
                            movieTitle
                        }
                    }
                }
                """
            },
            {
                "Media",
                "{ bookId: <Book>.id } | { movieId: <Movie>.id }",
                """
                {
                    ... on Book {
                        id
                    }
                    ... on Movie {
                        id
                    }
                }
                """
            },
            {
                "Media",
                "{ nested: { bookId: <Book>.id } | { movieId: <Movie>.id } }",
                """
                {
                    ... on Book {
                        id
                    }
                    ... on Movie {
                        id
                    }
                }
                """
            },
            {
                "Example",
                "a | b | c",
                """
                {
                    a
                    b
                    c
                }
                """
            }
        };
    }

    private static readonly ISchemaDefinition s_schema = SchemaParser.Parse(
        """
        type Query {
            mediaById(mediaId: ID!): Media
        }

        interface Media {
            id: ID!
        }

        type Book implements Media {
            id: ID!
            title: String!
            isbn: String!
            author: Author!
        }

        type Movie implements Media {
            id: ID!
            movieTitle: String!
            releaseDate: String!
        }

        type Author {
            id: ID!
            books: [Book!]!
        }

        type Product {
            dimensions: Dimensions!
            parts: [Part!]!
        }

        type Dimensions {
            size: Int!
            weight: Float!
        }

        type Part {
            id: ID!
            name: String!
        }

        type Location {
            coordinates: [Coordinate!]!
        }

        type Coordinate {
            x: Int!
            y: Int!
        }

        type Example {
            a: Int!
            b: Int!
            c: Int!
        }
        """);

    private static readonly SelectedValueToSelectionSetRewriter
        s_selectedValueToSelectionSetRewriter = new(s_schema);
}
