namespace HotChocolate.Language;

// Implements the parsing rules in the Operations section.
public ref partial struct Utf8GraphQLParser
{
    /// <summary>
    /// Parses a single schema coordinate.
    /// <see cref="SchemaCoordinateNode"  />:
    /// SchemaCoordinate :
    ///  - Name
    ///  - Name . Name
    ///  - Name . Name ( Name : )
    ///  - @ Name
    ///  - @ Name ( Name : )
    /// </summary>
    private SchemaCoordinateNode ParseSingleSchemaCoordinate()
    {
        var node = ParseSchemaCoordinate();
        Expect(TokenKind.EndOfFile);
        return node;
    }

    /// <summary>
    /// Parses a schema coordinate.
    /// <see cref="SchemaCoordinateNode"  />:
    /// SchemaCoordinate :
    ///  - Name
    ///  - Name . Name
    ///  - Name . Name ( Name : )
    ///  - @ Name
    ///  - @ Name ( Name : )
    /// </summary>
    private SchemaCoordinateNode ParseSchemaCoordinate()
    {
        var start = Start();

        var ofDirective = SkipAt();
        var name = ParseName();
        NameNode? memberName = null;
        NameNode? argumentName = null;

        if (SkipDot())
        {
            if (ofDirective)
            {
                throw Unexpected(TokenKind.Dot);
            }

            memberName = ParseName();
        }

        if (_reader.Kind == TokenKind.LeftParenthesis)
        {
            MoveNext();
            argumentName = ParseName();
            ExpectColon();
            ExpectRightParenthesis();
        }

        var location = CreateLocation(in start);

        return new SchemaCoordinateNode
        (
            location,
            ofDirective,
            name,
            memberName,
            argumentName
        );
    }
}
