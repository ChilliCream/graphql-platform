using System;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Pipeline.Complexity;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class OperationComplexityMiddleware
    {
        private readonly MaxComplexityVisitor _compiler = new MaxComplexityVisitor();
        private readonly RequestDelegate _next;
        private readonly DocumentValidatorContextPool _contextPool;

        public OperationComplexityMiddleware(
            RequestDelegate next,
            DocumentValidatorContextPool contextPool,
            IMaxComplexityOptionsAccessor options)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _contextPool = contextPool ??
                throw new ArgumentNullException(nameof(contextPool));
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {


        }
    }
}
