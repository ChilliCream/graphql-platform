using System.Collections.Generic;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ResolverStatistics
    {
        public IReadOnlyCollection<IPathSegment> Path { get; set; }

        public string ParentType { get; set; }

        public string FieldName { get; set; }

        public string ReturnType { get; set; }

        public long StartTimestamp { get; set; }

        public long EndTimestamp { get; set; }
    }
}
