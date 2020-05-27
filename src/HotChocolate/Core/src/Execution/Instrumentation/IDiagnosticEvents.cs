using System.Collections.Generic;

namespace HotChocolate.Execution.Instrumentation
{
    public interface IDiagnosticEvents
    {
        IActivityScope ParseDocument(IRequestContext context);

        IActivityScope ValidateDocument(IRequestContext context);

        void SyntaxError(IRequestContext context, IError error);

        void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors);
    }
}
