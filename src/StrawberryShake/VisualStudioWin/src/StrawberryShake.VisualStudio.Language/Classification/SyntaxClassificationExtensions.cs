using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public static class SyntaxClassificationExtensions
    {
        public static void AddClassification(
            this ICollection<SyntaxClassification> classifications,
            SyntaxClassificationKind kind,
            ISyntaxToken token) =>
            classifications.Add(new SyntaxClassification(
                kind, token.Start, token.Length));

        public static void AddClassification(
            this ICollection<SyntaxClassification> classifications,
            SyntaxClassificationKind kind,
            Location location) =>
            classifications.Add(new SyntaxClassification(
                kind, location.Start, location.Length));
    }
}
