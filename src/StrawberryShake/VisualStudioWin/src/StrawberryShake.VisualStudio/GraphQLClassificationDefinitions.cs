using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace StrawberryShake.VisualStudio
{
    internal static class GraphQLClassificationDefinitions
    {
        [Export]
        [Name("GraphQL")]
        internal static ClassificationTypeDefinition diffClassificationDefinition = null;

        [Export]
        [Name("GraphQL.Keyword")]
        [BaseDefinition("GraphQL")]
        internal static ClassificationTypeDefinition diffAddedDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "GraphQL.Keyword")]
        [Name("GraphQL.Keyword")]
        internal sealed class DiffAddedFormat : ClassificationFormatDefinition
        {
            public DiffAddedFormat()
            {
                ForegroundColor = Colors.Blue;
            }
        }
    }
}

