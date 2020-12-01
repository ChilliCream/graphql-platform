using System.Collections.Generic;

namespace StrawberryShake
{
    public class OperationFormatterOptions
    {
        public OperationFormatterOptions(
            IReadOnlyDictionary<string, object?>? extensions = null,
            bool? includeId = null,
            bool? includeDocument = null)
        {
            Extensions = extensions;
            IncludeId = includeId ?? true;
            IncludeDocument = includeDocument ?? true;
        }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public bool IncludeId { get; }

        public bool IncludeDocument { get; }

        public static OperationFormatterOptions Default { get; } =
            new OperationFormatterOptions();
    }
}
