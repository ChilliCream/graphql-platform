using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public partial class Utf8Parser
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NameNode ParseName(
            Utf8ParserContext context,
            ref Utf8GraphQLReader reader)
        {
            context.Start(ref reader);

            string name = ParserHelper.ExpectName(ref reader);
            Location location = context.CreateLocation(ref reader);

            return new NameNode
            (
                location,
                name
            );
        }
    }


}
