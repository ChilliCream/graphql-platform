using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

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

        internal static List<T> ParseMany<T>(
            ParserContext context,
            TokenKind openKind,
            Func<ParserContext, T> parser,
            TokenKind closeKind)
        {
            if (context.Current.Kind != openKind)
            {
                throw new SyntaxException(context,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        openKind,
                        context.Current));
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
