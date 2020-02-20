namespace StrawberryShake.CodeGeneration
{
    public class DocumentDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }

        public byte[] HashAlgorithm { get; }

        public byte[] Hash { get; }

        public byte[] Document { get; }

        public string OriginalDocument { get; }
    }
}
