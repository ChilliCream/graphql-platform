using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public interface IQueryContext
    {
        ISchema Schema { get; }
        IReadOnlyQueryRequest Request { get; }
        IRequestServiceScope ServiceScope { get; }
        IServiceProvider Services { get; }
        IDictionary<string, object> ContextData { get; }

        DocumentNode Document { get; set; }
        IOperation Operation { get; set; }
        QueryValidationResult ValidationResult { get; set; }
        IVariableCollection Variables { get; set; }
        CancellationToken RequestAborted { get; set; }
        IExecutionResult Result { get; set; }
        Exception Exception { get; set; }
    }
}
