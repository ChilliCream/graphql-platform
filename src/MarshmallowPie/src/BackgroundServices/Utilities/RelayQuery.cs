namespace MarshmallowPie.BackgroundServices
{
    public sealed class RelayQuery
    {
        public RelayQuery(
           string name,
           DocumentHash hash,
           string sourceText)
        {
            Name = name;
            Hash = hash;
            SourceText = sourceText;
        }

        public string Name { get; }

        public DocumentHash Hash { get; }

        public string SourceText { get; }
    }
}
