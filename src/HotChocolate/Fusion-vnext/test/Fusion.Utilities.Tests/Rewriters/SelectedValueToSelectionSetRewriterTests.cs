using HotChocolate.Fusion.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Fusion.Rewriters;

public sealed class SelectedValueToSelectionSetRewriterTests
{
    [Theory]
    [MemberData(nameof(ExamplesData))]
    public void Examples(string selectedValue, string selectionSet)
    {
        // arrange
        var selectedValueNode = new FieldSelectionMapParser(selectedValue).Parse();

        // act
        var selectionSetNode =
            SelectedValueToSelectionSetRewriter.SelectedValueToSelectionSet(selectedValueNode);

        // assert
        selectionSetNode.Print().MatchInlineSnapshot(selectionSet);
    }

    public static TheoryData<string, string> ExamplesData()
    {
        return new TheoryData<string, string>
        {
            {
                "id",
                """
                {
                    id
                }
                """
            },
            {
                "book.title",
                """
                {
                    book {
                        title
                    }
                }
                """
            },
            {
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
            // TODO: Test "dimension.{ size weight }" (not yet supported by parser).
            // FIXME: Waiting for selection set merge utility.
            {
                "{ size: dimensions.size weight: dimensions.weight }",
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
                "parts[{ id name }]",
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
                "parts[[{ id name }]]",
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
                "{ coordinates: coordinates[{ lat: x lon: y }] }",
                """
                {
                    coordinates {
                        x
                        y
                    }
                }
                """
            },
            // FIXME: Waiting for selection set merge utility.
            {
                "mediaById<Book>.title | mediaById<Movie>.movieTitle",
                """
                {
                    mediaById {
                        ... on Book {
                            title
                        }
                        ... on Movie {
                            title
                        }
                    }
                }
                """
            },
            {
                "{ movieId: <Movie>.id } | { productId: <Product>.id }",
                """
                {
                    ... on Movie {
                        id
                    }
                    ... on Product {
                        id
                    }
                }
                """
            },
            {
                "{ nested: { movieId: <Movie>.id } | { productId: <Product>.id } }",
                """
                {
                    ... on Movie {
                        id
                    }
                    ... on Product {
                        id
                    }
                }
                """
            },
            {
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
}
