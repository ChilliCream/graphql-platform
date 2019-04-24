using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public partial class Utf8Parser
    {
        private static NameNode ParseName(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader)
        {
            context.Start(in reader);

            string name = ParserHelper.ExpectName(in reader);
            Location location = context.CreateLocation(in reader);

            return new NameNode
            (
                location,
                name
            );
        }

        private static List<T> ParseMany<T>(
            Utf8ParserContext context,
            in Utf8GraphQLReader reader,
            TokenKind openKind,
            Func<T> parser,
            TokenKind closeKind)
        {
            if (reader.Kind != openKind)
            {
                throw new SyntaxException(reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        openKind,
                        TokenVisualizer.Visualize(reader)));
            }

            var list = new List<T>();

            // skip opening token
            reader.Read();

            while (reader.Kind != closeKind)
            {
                list.Add(parser());
            }

            // skip closing token
            ParserHelper.Expect(in reader, closeKind);

            return list;
        }
    }


}
