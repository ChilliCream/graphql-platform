using System.Collections.Generic;
using System.Linq;
using Generator.ClassGenerator;

namespace Generator
{
    internal class QueryValidation : IAction
    {
        private readonly Dictionary<string, string> _validationRuleStatements =
            new Dictionary<string, string>
        {
            {"ExecutableDefinitions", "new ExecutableDefinitionsRule()" }
        };

        private readonly List<string> _validationRules;

        public QueryValidation(object value)
        {
            _validationRules = value as List<string>;
        }

        public Block CreateBlock(Statement header)
        {
            IEnumerable<string> rules = _validationRuleStatements
                .Where(pair => _validationRules.Contains(pair.Key))
                .Select(pair => pair.Value);

            var validationDefinition = new Statement(
                $"var validationRules = new List<IQueryValidationRule> {{ {string.Join(",", rules)} }}");

            var queryValidatorDefinition = new Statement(
                "var validator = new QueryValidator(validationRules)");

            var queryValidatorAction = new Statement(
                "var result = validator.Validate(_schema, query)");

            return new Block
            {
                header,
                validationDefinition,
                queryValidatorDefinition,
                queryValidatorAction
            };
        }
    }
}
