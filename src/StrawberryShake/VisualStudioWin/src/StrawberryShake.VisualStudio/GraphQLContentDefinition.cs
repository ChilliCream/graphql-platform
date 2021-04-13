using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace StrawberryShake.VisualStudio
{
    public class BarContentDefinition
    {
        [Export]
        [Name("graphql")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition GraphQLContentTypeDefinition;

        [Export]
        [FileExtension(".graphql")]
        [ContentType("graphql")]
        internal static FileExtensionToContentTypeDefinition GraphQLFileExtensionDefinition;
    }
}
