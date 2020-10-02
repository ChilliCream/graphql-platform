using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class OperationContext
    {
        public ISchema Schema => _requestContext.Schema;

        public IErrorHandler ErrorHandler => _requestContext.ErrorHandler;

        public ITypeConverter Converter => _requestContext.Converter;

        public IActivator Activator => _requestContext.Activator;

        public IDiagnosticEvents DiagnosticEvents => _requestContext.DiagnosticEvents;

        public IDictionary<string, object?> ContextData => _requestContext.ContextData;

        public CancellationToken RequestAborted => _requestContext.RequestAborted;
    }
}
