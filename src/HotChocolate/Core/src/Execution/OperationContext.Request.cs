using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed partial class OperationContext : IOperationContext
    {
        public ISchema Schema => _requestContext.Schema;

        public IServiceProvider Services => _requestContext.Services;

        public IErrorHandler ErrorHandler => _requestContext.ErrorHandler;

        public ITypeConversion Converter => _requestContext.Converter;

        public IDictionary<string, object?> ContextData => _requestContext.ContextData;

        public CancellationToken RequestAborted => _requestContext.RequestAborted;
    }
}