using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace StrawberryShake.VisualStudio
{
    internal static class GraphQL
    {
        internal const string ContentType = "application/graphql";

        internal const string FileExtension = ".graphql";

        [Export]
        [Name(ContentType)]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition ContentTypeDefinition;

        [Export]
        [Name(ContentType + nameof(FileExtensionToContentTypeDefinition))]
        [ContentType(ContentType)]
        [FileExtension(FileExtension)]
        internal static FileExtensionToContentTypeDefinition FileExtensionToContentTypeDefinition;
    }
}
