using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResolverRecord
    {
        internal ApolloTracingResolverRecord() { }

        public ApolloTracingResolverRecord(IResolverContext context)
        {
            Path = context.Path.ToCollection();
            ParentType = context.ObjectType.Name;
            FieldName = context.Field.Name;
            ReturnType = context.Field.Type.TypeName();
        }

        public IReadOnlyCollection<object> Path { get; internal set; }

        public string ParentType { get; internal set; }

        public string FieldName { get; internal set; }

        public string ReturnType { get; internal set; }

        public long StartTimestamp { get; set; }

        public long EndTimestamp { get; set; }
    }
}
