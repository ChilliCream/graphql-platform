using HotChocolate.Language;

namespace MarshmallowPie.Processing
{
    public class DocumentInfo
    {
        public DocumentInfo(
            string name,
            string? hash,
            string? hashAlgorithm,
            HashFormat? hashFormat)
        {
            Name = name;
            Hash = hash;
            HashAlgorithm = hashAlgorithm;
            HashFormat = hashFormat;
        }

        public string Name { get; }

        public string? Hash { get; }

        public string? HashAlgorithm { get; }

        public HashFormat? HashFormat { get; }
    }
}
