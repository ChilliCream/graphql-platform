using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public interface IQueryContext
        : IHasContextData
    {
        ISchema Schema { get; }
        IReadOnlyQueryRequest Request { get; set; }
        IRequestServiceScope ServiceScope { get; }
        IServiceProvider Services { get; }

        DocumentNode Document { get; set; }
        IOperation Operation { get; set; }
        QueryValidationResult ValidationResult { get; set; }
        IVariableCollection Variables { get; set; }
        CancellationToken RequestAborted { get; set; }
        IExecutionResult Result { get; set; }
        Exception Exception { get; set; }
        Func<FieldSelection, FieldDelegate> MiddlewareResolver { get; set; }
    }
}
