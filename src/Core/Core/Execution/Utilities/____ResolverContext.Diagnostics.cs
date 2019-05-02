using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal partial class ____ResolverContext
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
