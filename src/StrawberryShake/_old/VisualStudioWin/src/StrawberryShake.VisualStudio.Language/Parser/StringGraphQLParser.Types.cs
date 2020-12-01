namespace StrawberryShake.VisualStudio.Language
{
    // Implements the parsing rules in the Types section.
    public ref partial struct StringGraphQLParser
    {
        /// <summary>
        /// Parses a type reference.
        /// <see cref="ITypeNode" />:
        /// - NamedType
        /// - ListType
        /// - NonNullType
        /// </summary>
        /// <param name="context">The parser context.</param>
        private ITypeNode ParseTypeReference()
        {
            ITypeNode type;
            Location location;

            if (_reader.Kind == TokenKind.LeftBracket)
            {
                ISyntaxToken start = _reader.Token;

                MoveNext();
                type = ParseTypeReference();
                ExpectRightBracket();

                location = new Location(start, _reader.Token);

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
                    ISyntaxToken start = _reader.Token;
                    MoveNext();
                    location = new Location(start, _reader.Token);

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
        /// <param name="context">The parser context.</param>
        private NamedTypeNode ParseNamedType()
        {
            ISyntaxToken start = _reader.Token;
            NameNode name = ParseName();
            var location = new Location(start, _reader.Token);

            return new NamedTypeNode
            (
                location,
                name
            );
        }
    }
}
