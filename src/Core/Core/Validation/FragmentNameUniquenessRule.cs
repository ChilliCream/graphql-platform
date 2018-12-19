using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Fragment definitions are referenced in fragment spreads by name.
    /// To avoid ambiguity, each fragment’s name must be unique within a
    /// document.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Fragment-Name-Uniqueness
    /// </summary>
    internal sealed class FragmentNameUniquenessRule
        : IQueryValidationRule
    {
        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            var errors = new Dictionary<string, ValidationError>();
            var fragmentNames = new HashSet<string>();

            foreach (FragmentDefinitionNode fragment in queryDocument
                .Definitions.OfType<FragmentDefinitionNode>())
            {
                if (fragmentNames.Contains(fragment.Name.Value)
                    && !errors.ContainsKey(fragment.Name.Value))
                {
                    errors[fragment.Name.Value] = new ValidationError(
                        "There are multiple fragments with the name " +
                        $"`{fragment.Name.Value}`.",
                        fragment);
                }
                fragmentNames.Add(fragment.Name.Value);
            }

            return new QueryValidationResult(errors.Values);
        }
    }
}
