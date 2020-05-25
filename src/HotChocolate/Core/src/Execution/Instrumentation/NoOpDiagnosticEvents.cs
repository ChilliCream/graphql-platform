using System.Collections.Generic;

namespace HotChocolate.Execution.Instrumentation
{
    internal sealed class NoopDiagnosticEvents
        : IDiagnosticEvents
        , IActivityScope
    {
        public IActivityScope ParseDocument(
            IRequestContext context) => this;

        public void SyntaxError(
            IRequestContext context, 
            IError error) 
        { }

        public IActivityScope ValidateDocument(
            IRequestContext context) => this;

        public void ValidationErrors(
            IRequestContext context, 
            IReadOnlyList<IError> errors) 
        { }
        
        public void Dispose() { }
    }
}
