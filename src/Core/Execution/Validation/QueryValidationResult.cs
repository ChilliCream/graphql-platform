using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Execution.Validation
{
    public class QueryValidationResult
    {
        private QueryValidationResult()
        {
            Errors = Array.Empty<IQueryError>();
            HasErrors = false;
        }

        public QueryValidationResult(IQueryError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            Errors = new IQueryError[] { error };
            HasErrors = true;
        }

        public QueryValidationResult(IEnumerable<IQueryError> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            Errors = errors.ToArray();
            HasErrors = errors.Any();
        }

        public bool HasErrors { get; }

        public IReadOnlyCollection<IQueryError> Errors { get; }

        public static QueryValidationResult OK { get; } = new QueryValidationResult();
    }
}
