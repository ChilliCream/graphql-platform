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
    }


}
