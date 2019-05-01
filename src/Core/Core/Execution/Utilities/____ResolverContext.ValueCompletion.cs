using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal partial class ____ResolverContext
    {
        public ResolverTask Branch(
            FieldSelection fieldSelection,
            Path path,
            IImmutableStack<object> source,
            IDictionary<string, object> result,
            Action propagateNonNullViolation)
        {
            ResolverTask branch = ObjectPools.ResolverTasks.Rent();
            branch.Initialize(
                this,
                fieldSelection,
                path,
                source,
                resolverResult,
                result,
                propagateNonNullViolation);
            return branch;
        }

        public void SetCompletedValue(object completedValue)
        {
            _serializedResult[ResponseName] = completedValue;
        }
    }
}
