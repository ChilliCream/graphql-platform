using System.Collections.Generic;

namespace StrawberryShake
{
    public class OperationFormatterOptions
    {
        public OperationFormatterOptions(
            IReadOnlyDictionary<string, object?>? extensions = null,
            bool? includeDocument = null)
        {
            Extensions = extensions;
            IncludeDocument = includeDocument ?? true;
        }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public bool IncludeDocument { get; }

        public static OperationFormatterOptions Default { get; } =
            new OperationFormatterOptions();
    }
}
