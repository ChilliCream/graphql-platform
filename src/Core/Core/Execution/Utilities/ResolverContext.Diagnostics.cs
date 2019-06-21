using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution
{
    internal partial class ResolverContext
    {
        public QueryExecutionDiagnostics Diagnostics =>
            _executionContext.Diagnostics;

        public Activity BeginResolveField() =>
            Diagnostics.BeginResolveField(this);

        public void ResolverError(IError error) =>
            Diagnostics.ResolverError(this, new[] { error });

        public void ResolverError(IEnumerable<IError> errors) =>
            Diagnostics.ResolverError(this, errors);

        public void EndResolveField(Activity activity) =>
            Diagnostics.EndResolveField(activity, this, Result);
    }
}
