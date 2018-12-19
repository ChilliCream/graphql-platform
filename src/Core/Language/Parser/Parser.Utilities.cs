using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public partial class Parser
    {
        internal static NameNode ParseName(ParserContext context)
        {
            SyntaxToken token = context.ExpectName();
            Location location = context.CreateLocation(token);

            return new NameNode
            (
                location,
                token.Value
            );
        }

        // TODO : move into separate parser utilities class
        internal static List<T> ParseMany<T>(
            ParserContext context,
            TokenKind openKind,
            Func<ParserContext, T> parser,
            TokenKind closeKind)
        {
            if (context.Current.Kind != openKind)
            {
                throw new SyntaxException(context,
                    $"Expected a name token: {context.Current}.");
            }

            var list = new List<T>();

            // skip opening token
            context.MoveNext();

            while (context.Current.Kind != closeKind)
            {
                list.Add(parser(context));
            }

            // skip closing token
            context.MoveNext();

            return list;
        }
    }
}
