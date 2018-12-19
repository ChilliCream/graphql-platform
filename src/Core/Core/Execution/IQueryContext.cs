using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    public interface IQueryContext
    {
        ISchema Schema { get; }
        IReadOnlyQueryRequest Request { get; }
        IServiceProvider Services { get; }
        IDictionary<string, object> Custom { get; }

        DocumentNode Document { get; set; }
        OperationDefinitionNode Operation { get; set; }
        QueryValidationResult ValidationResult { get; set; }
        IVariableCollection VariableCollection { get; set; }
        CancellationToken RequestAborted { get; set; }
        IExecutionResult Result { get; set; }
        Exception Exception { get; set; }
    }
}
