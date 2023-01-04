using System.Collections.Generic;

namespace HotChocolate.Validation.Options;

public interface IValidationConfiguration
{
    IEnumerable<IDocumentValidatorRule> GetRules(string schemaName);

    IEnumerable<IValidationResultAggregator> GetPostRules(string schemaName);

    ValidationOptions GetOptions(string schemaName);
}
