using System;
using System.Collections.Immutable;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
    {
        public ResolverContext Branch(
            FieldSelection fieldSelection,
            IImmutableStack<object> source,
            object sourceObject,
            FieldData serializedResult,
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
            _serializedResult.SetFieldValue(ResponseIndex, ResponseName, completedValue);
        }
    }
}
