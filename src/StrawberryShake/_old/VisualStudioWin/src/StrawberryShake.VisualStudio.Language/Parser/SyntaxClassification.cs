namespace StrawberryShake.VisualStudio.Language
{
    public readonly struct SyntaxClassification
    {
        public SyntaxClassification(SyntaxClassificationKind kind, int start, int length)
        {
            Kind = kind;
            Start = start;
            End = start + length;
            Length = length;
        }

        public SyntaxClassificationKind Kind { get; }
        public int Start { get; }
        public int End{ get; }
        public int Length { get; }
    }

}
