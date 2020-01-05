namespace StrawberryShake.VisualStudio.Language
{
    public readonly struct SyntaxClassification
    {
        public SyntaxClassification(SyntaxClassificationKind kind, int start, int length)
        {
            Kind = kind;
            Start = start;
            Length = length;
        }

        public SyntaxClassificationKind Kind { get; }
        public int Start { get; }
        public int Length { get; }
    }

}
