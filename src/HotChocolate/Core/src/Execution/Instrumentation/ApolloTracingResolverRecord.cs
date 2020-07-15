using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Instrumentation
{
    internal class ApolloTracingResolverRecord
    {
        public ApolloTracingResolverRecord(
            IResolverContext context, 
            long startTimestamp, 
            long endTimestamp)
        {
            Path = context.Path.ToList();
            ParentType = context.ObjectType.Name;
            FieldName = context.Field.Name;
            ReturnType = context.Field.Type.TypeName();
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
        }

        public IReadOnlyList<object> Path { get; }

        public string ParentType { get; }

        public string FieldName { get; }

        public string ReturnType { get; }

        public long StartTimestamp { get; }

        public long EndTimestamp { get; }
    }
}
