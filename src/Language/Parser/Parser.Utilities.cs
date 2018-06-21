using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    public partial class Parser
    {
        private NameNode ParseName(ParserContext context)
        {
            SyntaxToken token = context.ExpectName();
            Location location = context.CreateLocation(token);

            return new NameNode
            (
                location,
                token.Value
            );
        }

        private List<T> ParseMany<T>(
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

            List<T> list = new List<T>();

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
