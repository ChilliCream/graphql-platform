#pragma warning disable CA1812
#nullable disable

using System.Collections.Generic;

namespace HotChocolate.Utilities.Introspection
{
    internal class IntrospectionResult
    {
        public IntrospectionData Data { get; set; }

        public List<IntrospectionError> Errors { get; set; }
    }
}
#pragma warning restore CA1812
