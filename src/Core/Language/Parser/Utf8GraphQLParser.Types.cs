namespace HotChocolate.Language
{
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
        /// <param name="context">The parser context.</param>
        private ITypeNode ParseTypeReference()
        {
            ITypeNode type;
            Location? location;

            if (_reader.Kind == TokenKind.LeftBracket)
            {
                TokenInfo start = Start();

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
                    TokenInfo start = Start();
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
        /// <param name="context">The parser context.</param>
        private NamedTypeNode ParseNamedType()
        {
            TokenInfo start = Start();
            NameNode name = ParseName();
            Location? location = CreateLocation(in start);

            return new NamedTypeNode
            (
                location,
                name
            );
        }
    }
}
