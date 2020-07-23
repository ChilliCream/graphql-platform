using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
    {
        public ResolverContext Branch(
            IPreparedSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            IDictionary<string, object> serializedResult,
            Path path,
            Action propagateNonNullViolation)
        {
            ResolverContext branch = Rent(
                fieldSelection,
                source, sourceObject,
                this,
                serializedResult,
                path,
                propagateNonNullViolation);
            return branch;
        }

        public void SetCompletedValue(object completedValue)
        {
            _serializedResult[ResponseName] = completedValue;
        }
    }
}
