using System;
using System.Collections.Generic;
using System.Linq;
using Generator.ClassGenerator;

namespace Generator
{
    internal class QueryValidation : IAction
    {
        private readonly List<string> _validationRules;

        public QueryValidation(object value)
        {
            var validationRules = value as List<object>;
            if (validationRules == null)
            {
                throw new InvalidOperationException("Invalid validation structure");
            }

            _validationRules = validationRules
                .Select(r => r as string)
                .ToList();
        }


        public Block CreateBlock()
        {
            var queryValidatorDefinition = new Statement(
                "IQueryValidator validator = _serviceProvider.GetService<IQueryValidator>();");

            var queryValidatorAction = new Statement(
                "QueryValidationResult result = validator.Validate(_schema, _parser.Parse(query));");

            return new Block
            {
                queryValidatorDefinition,
                queryValidatorAction
            };
        }
    }
}
