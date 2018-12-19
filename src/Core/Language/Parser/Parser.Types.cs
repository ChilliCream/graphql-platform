namespace HotChocolate.Language
{
    // Implements the parsing rules in the Types section.
    public partial class Parser
    {
        /// <summary>
        /// Parses a type reference.
        /// <see cref="ITypeNode" />:
        /// - NamedType
        /// - ListType
        /// - NonNullType
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ITypeNode ParseTypeReference(ParserContext context)
        {
            SyntaxToken start = context.Current;
            ITypeNode type;
            Location location;

            if (context.Skip(TokenKind.LeftBracket))
            {
                type = ParseTypeReference(context);
                context.ExpectRightBracket();
                location = context.CreateLocation(start);

                type = new ListTypeNode(location, type);
            }
            else
            {
                type = ParseNamedType(context);
            }

            if (context.Skip(TokenKind.Bang))
            {
                if (type is INullableType nt)
                {
                    return new NonNullTypeNode
                    (
                        context.CreateLocation(start),
                        nt
                    );
                }
                context.Unexpected(context.Current.Previous);
            }

            return type;
        }

        /// <summary>
        /// Parses a named type.
        /// <see cref="NamedTypeNode" />:
        /// Name
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static NamedTypeNode ParseNamedType(ParserContext context)
        {
            SyntaxToken start = context.Current;
            NameNode name = ParseName(context);
            Location location = context.CreateLocation(start);

            return new NamedTypeNode
            (
                location,
                name
            );
        }
    }
}
