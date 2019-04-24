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
            in Utf8GraphQLReader reader)
        {
            ITypeNode type;
            Location location;

            if (reader.Kind == TokenKind.LeftBracket)
            {
                context.Start(in reader);
                reader.Read();
                type = ParseTypeReference(context, in reader);
                ParserHelper.ExpectRightBracket(in reader);
                location = context.CreateLocation(in reader);
                type = new ListTypeNode(location, type);
            }
            else
            {
                type = ParseNamedType(context, in reader);
            }

            if (reader.Kind == TokenKind.Bang)
            {
                if (type is INullableTypeNode nt)
                {
                    context.Start(in reader);
                    reader.Read();
                    return new NonNullTypeNode
                    (
                        context.CreateLocation(in reader),
                        nt
                    );
                }
                ParserHelper.Unexpected(in reader, TokenKind.Bang);
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
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);
            NameNode name = ParseName(context, in reader);
            Location location = context.CreateLocation(in reader);

            return new NamedTypeNode
            (
                location,
                name
            );
        }
    }
}
