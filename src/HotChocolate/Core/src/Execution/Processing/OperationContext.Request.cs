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
                AssertNotPooled();
                return _requestContext.Schema;
            }
        }

        public IErrorHandler ErrorHandler
        {
            get
            {
                AssertNotPooled();
                return _requestContext.ErrorHandler;
            }
        }

        public ITypeConverter Converter
        {
            get
            {
                AssertNotPooled();
                return _requestContext.Converter;
            }
        }

        public IActivator Activator
        {
            get
            {
                AssertNotPooled();
                return _requestContext.Activator;
            }
        }

        public IDiagnosticEvents DiagnosticEvents
        {
            get
            {
                AssertNotPooled();
                return _requestContext.DiagnosticEvents;
            }
        }

        public IDictionary<string, object?> ContextData
        {
            get
            {
                AssertNotPooled();
                return _requestContext.ContextData;
            }
        }

        public CancellationToken RequestAborted
        {
            get
            {
                AssertNotPooled();
                return _requestContext.RequestAborted;
            }
        }
    }
}
