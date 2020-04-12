namespace HotChocolate.Language.Visitors
{
    public struct SyntaxVisitorOptions
    {
        public bool VisitNames { get; set; }

        public bool VisitDescriptions { get; set; }

        public bool VisitDirectives { get; set; }

        public bool VisitArguments { get; set; }
    }
}
