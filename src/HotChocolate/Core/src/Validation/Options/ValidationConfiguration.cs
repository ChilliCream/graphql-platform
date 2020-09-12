using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace HotChocolate.Validation.Options
{
    public class ValidationConfiguration
        : IValidationConfiguration
    {
        private readonly ConcurrentDictionary<string, ValidationOptions> _optionsCache =
            new ConcurrentDictionary<string, ValidationOptions>();
        private readonly IOptionsMonitor<ValidationOptionsModifiers> _optionsMonitor;

        public ValidationConfiguration(IOptionsMonitor<ValidationOptionsModifiers> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor
                ?? throw new ArgumentNullException(nameof(optionsMonitor));
        }

        public IEnumerable<IDocumentValidatorRule> GetRules(string schemaName)
        {
            ValidationOptions options =_optionsCache.GetOrAdd(schemaName, CreateOptions);
            return options.Rules;
        }

        private ValidationOptions CreateOptions(string schemaName)
        {
            ValidationOptionsModifiers modifiers = _optionsMonitor.Get(schemaName);
            var options = new ValidationOptions();

            for (int i = 0; i < modifiers.Modifiers.Count; i++)
            {
                modifiers.Modifiers[i](options);
            }

            return options;
        }
    }
}
