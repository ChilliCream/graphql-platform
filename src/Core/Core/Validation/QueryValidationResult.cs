using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Validation
{
    public class QueryValidationResult
    {
        private QueryValidationResult()
        {
            Errors = Array.Empty<IError>();
            HasErrors = false;
        }

        public QueryValidationResult(IError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors = new IError[] { error };
            HasErrors = true;
        }

        public QueryValidationResult(IEnumerable<IError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToArray();
            HasErrors = errors.Any();
        }

        public bool HasErrors { get; }

        public IReadOnlyCollection<IError> Errors { get; }

        public static QueryValidationResult OK { get; } = new QueryValidationResult();
    }
}
