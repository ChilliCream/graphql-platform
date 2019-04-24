namespace HotChocolate.Language
{
    // Implements the parsing rules in the Types section.
    public partial class Utf8Parser
    {
        /// <summary>
        /// Parses a type reference.
        /// <see cref="ITypeNode" />:
        /// - NamedType
        /// - ListType
        /// - NonNullType
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static ITypeNode ParseTypeReference(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            ITypeNode type;
            Location location;

            if (reader.Kind == TokenKind.LeftBracket)
            {
                context.Start(ref reader);
                reader.Read();
                type = ParseTypeReference(context, ref reader);
                ParserHelper.ExpectRightBracket(ref reader);
                location = context.CreateLocation(ref reader);
                type = new ListTypeNode(location, type);
            }
            else
            {
                type = ParseNamedType(context, ref reader);
            }

            if (reader.Kind == TokenKind.Bang)
            {
                if (type is INullableTypeNode nt)
                {
                    context.Start(ref reader);
                    reader.Read();
                    return new NonNullTypeNode
                    (
                        context.CreateLocation(ref reader),
                        nt
                    );
                }
                ParserHelper.Unexpected(ref reader, TokenKind.Bang);
            }

            return type;
        }

        /// <summary>
        /// Parses a named type.
        /// <see cref="NamedTypeNode" />:
        /// Name
        /// </summary>
        /// <param name="context">The parser context.</param>
        private static NamedTypeNode ParseNamedType(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);
            NameNode name = ParseName(context, ref reader);
            Location location = context.CreateLocation(ref reader);

            return new NamedTypeNode
            (
                location,
                name
            );
        }
    }
}
