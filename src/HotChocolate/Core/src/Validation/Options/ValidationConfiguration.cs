using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace HotChocolate.Validation.Options;

public class ValidationConfiguration : IValidationConfiguration
{
    private readonly ConcurrentDictionary<string, ValidationOptions> _optionsCache = new();
    private readonly IOptionsMonitor<ValidationOptionsModifiers> _optionsMonitor;

    public ValidationConfiguration(IOptionsMonitor<ValidationOptionsModifiers> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor
            ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    public IEnumerable<IDocumentValidatorRule> GetRules(string schemaName)
        => GetOptions(schemaName).Rules;

    public IEnumerable<IValidationResultAggregator> GetResultAggregators(string schemaName)
        => GetOptions(schemaName).ResultAggregators;

    public ValidationOptions GetOptions(string schemaName)
        => _optionsCache.GetOrAdd(schemaName, CreateOptions);

    private ValidationOptions CreateOptions(string schemaName)
    {
        var modifiers = _optionsMonitor.Get(schemaName);
        var options = new ValidationOptions();

        for (var i = 0; i < modifiers.Modifiers.Count; i++)
        {
            modifiers.Modifiers[i](options);
        }

        return options;
    }
}
