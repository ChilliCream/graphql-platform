using System.Collections.Generic;

namespace HotChocolate.Validation.Options
{
    public interface IValidationConfiguration
    {
        IEnumerable<IDocumentValidatorRule> GetRules(string schemaName);
    }
}
