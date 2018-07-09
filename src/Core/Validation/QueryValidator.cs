using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    public class QueryValidator
    {
        private static readonly IQueryValidationRule[] _rules =
            new IQueryValidationRule[]
            {
                new ExecutableDefinitionsRule(),
                new LoneAnonymousOperationRule(),
                new OperationNameUniquenessRule()
            };

        private readonly ISchema _schema;

        public QueryValidator(ISchema schema)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
        }

        public QueryValidationResult Validate(DocumentNode query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            List<IQueryError> errors = new List<IQueryError>();
            foreach (IQueryValidationRule rule in _rules)
            {
                QueryValidationResult result = rule.Validate(_schema, query);
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
