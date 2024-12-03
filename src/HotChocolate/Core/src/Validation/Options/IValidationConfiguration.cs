namespace HotChocolate.Validation.Options;

public interface IValidationConfiguration
{
    IEnumerable<IDocumentValidatorRule> GetRules(string schemaName);

    IEnumerable<IValidationResultAggregator> GetResultAggregators(string schemaName);

    ValidationOptions GetOptions(string schemaName);
}
