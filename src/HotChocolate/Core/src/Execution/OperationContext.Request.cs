using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed partial class OperationContext : IOperationContext
    {
        private IRequestContext _requestContext = default!;

        public ISchema Schema => _requestContext.Schema;

        public IServiceProvider Services => _requestContext.Services;

        public IErrorHandler ErrorHandler => _requestContext.ErrorHandler;

        public ITypeConversion Converter => _requestContext.Converter;

        public IDictionary<string, object?> ContextData => _requestContext.ContextData;

        public CancellationToken RequestAborted => _requestContext.RequestAborted;
    }
}