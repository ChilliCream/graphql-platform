using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public class QueryValidator
        : IQueryValidator
    {
        private static IQueryValidationRule[] _rules;

        public QueryValidator(IEnumerable<IQueryValidationRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _rules = rules.ToArray();
        }

        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            List<IError> errors = new List<IError>();
            for (int i = 0; i < _rules.Length; i++)
            {
                QueryValidationResult result =
                    _rules[i].Validate(schema, query);
                if (result.HasErrors)
                {
                    errors.AddRange(result.Errors);
                }
            }

            if (errors.Any())
            {
                return new QueryValidationResult(errors);
            }
            return QueryValidationResult.OK;
        }
    }
}
