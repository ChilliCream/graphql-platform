using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace HotChocolate.Validation.Options;

public class ValidationConfiguration(
    IOptionsMonitor<ValidationOptionsModifiers> optionsMonitor)
    : IValidationConfiguration
{
    private readonly ConcurrentDictionary<string, (ValidationOptions, ValidationRulesOptions)> _optionsCache = new();
    private readonly IOptionsMonitor<ValidationOptionsModifiers> _optionsMonitor = optionsMonitor
        ?? throw new ArgumentNullException(nameof(optionsMonitor));

    public IEnumerable<IDocumentValidatorRule> GetRules(string schemaName)
        => GetRulesOptions(schemaName).Rules;

    public IEnumerable<IValidationResultAggregator> GetResultAggregators(string schemaName)
        => GetRulesOptions(schemaName).ResultAggregators;

    public ValidationOptions GetOptions(string schemaName)
        => _optionsCache.GetOrAdd(schemaName, CreateOptions).Item1;

    private ValidationRulesOptions GetRulesOptions(string schemaName)
        => _optionsCache.GetOrAdd(schemaName, CreateOptions).Item2;

    private (ValidationOptions, ValidationRulesOptions) CreateOptions(string schemaName)
    {
        var modifiers = _optionsMonitor.Get(schemaName);
        var options = new ValidationOptions();
        var rulesOptions = new ValidationRulesOptions();

        for (var i = 0; i < modifiers.Modifiers.Count; i++)
        {
            modifiers.Modifiers[i](options);
        }

        for (var i = 0; i < modifiers.RulesModifiers.Count; i++)
        {
            modifiers.RulesModifiers[i](options, rulesOptions);
        }

        return (options, rulesOptions);
    }
}
