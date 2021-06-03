using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing
{
    internal sealed partial class OperationContext
    {
        public ISchema Schema
        {
            get
            {
                AssertInitialized();
                return _requestContext.Schema;
            }
        }

        public IErrorHandler ErrorHandler
        {
            get
            {
                AssertInitialized();
                return _requestContext.ErrorHandler;
            }
        }

        public ITypeConverter Converter
        {
            get
            {
                AssertInitialized();
                return _requestContext.Converter;
            }
        }

        public IActivator Activator
        {
            get
            {
                AssertInitialized();
                return _requestContext.Activator;
            }
        }

        public IDiagnosticEvents DiagnosticEvents
        {
            get
            {
                AssertInitialized();
                return _requestContext.DiagnosticEvents;
            }
        }

        public IDictionary<string, object?> ContextData
        {
            get
            {
                AssertInitialized();
                return _requestContext.ContextData;
            }
        }

        public CancellationToken RequestAborted
        {
            get
            {
                AssertInitialized();
                return _requestContext.RequestAborted;
            }
        }
    }
}
