using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using StrawberryShake.VisualStudio.Language;

namespace StrawberryShake.VisualStudio
{
    internal static class ListExtensions
    {
        public static void AddClassification(
            this ICollection<ClassificationSpan> classifications,
            SnapshotSpan snapshotSpan,
            SyntaxClassification classification,
            IClassificationType type)
        {
            var span = new Span(classification.Start, classification.Length);
            classifications.Add(new ClassificationSpan(
                new SnapshotSpan(snapshotSpan.Snapshot, span),
                type));
        }
    }
}
