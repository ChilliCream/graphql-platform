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
        public ____ResolverContext Branch(
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            IDictionary<string, object> serializedResult,
            Path path,
            Action propagateNonNullViolation)
        {
            ____ResolverContext branch = Rent(
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
