using System.ComponentModel;
using HotChocolate.Language;

namespace MarshmallowPie.GraphQL.Clients
{
    public class QueryFile
    {
        public QueryFile(
            string name,
            string sourceText,
            string? hash,
            string hashAlgorithm,
            HashFormat hashFormat)
        {
            Name = name;
            SourceText = sourceText;
            Hash = hash;
            HashAlgorithm = hashAlgorithm;
            HashFormat = hashFormat;
        }

        public string Name { get; }

        public string SourceText { get; }

        public string? Hash { get; }

        [DefaultValue("MD5")]
        public string HashAlgorithm { get; }

        [DefaultValue(HashFormat.Hex)]
        public HashFormat HashFormat { get; }
    }
}
