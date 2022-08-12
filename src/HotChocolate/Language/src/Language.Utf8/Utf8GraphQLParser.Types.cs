namespace HotChocolate.Language;

// Implements the parsing rules in the Types section.
public ref partial struct Utf8GraphQLParser
{
    /// <summary>
    /// Parses a type reference.
    /// <see cref="ITypeNode" />:
    /// - NamedType
    /// - ListType
    /// - NonNullType
    /// </summary>
    private ITypeNode ParseTypeReference()
    {
        ITypeNode type;
        Location? location;

        if (_reader.Kind == TokenKind.LeftBracket)
        {
            var start = Start();

            MoveNext();
            type = ParseTypeReference();
            ExpectRightBracket();

            location = CreateLocation(in start);

            type = new ListTypeNode(location, type);
        }
        else
        {
            type = ParseNamedType();
        }

        if (_reader.Kind == TokenKind.Bang)
        {
            if (type is INullableTypeNode nt)
            {
                var start = Start();
                MoveNext();
                location = CreateLocation(in start);

                return new NonNullTypeNode
                (
                    location,
                    nt
                );
            }

            Unexpected(TokenKind.Bang);
        }

        return type;
    }

    /// <summary>
    /// Parses a named type.
    /// <see cref="NamedTypeNode" />:
    /// Name
    /// </summary>
    private NamedTypeNode ParseNamedType()
    {
        var start = Start();
        var name = ParseName();
        var location = CreateLocation(in start);

        return new NamedTypeNode
        (
            location,
            name
        );
    }
}
