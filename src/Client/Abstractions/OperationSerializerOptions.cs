using System.Collections.Generic;

namespace StrawberryShake
{
    public class OperationSerializerOptions
    {
        public OperationSerializerOptions(
            IReadOnlyDictionary<string, object?>? extensions = null,
            bool? includeDocument = null)
        {
            Extensions = extensions;
            IncludeDocument = includeDocument ?? true;
        }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public bool IncludeDocument { get; }

        public static OperationSerializerOptions Default { get; } =
            new OperationSerializerOptions();
    }
}
