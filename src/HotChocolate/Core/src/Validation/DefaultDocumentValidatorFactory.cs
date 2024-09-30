using HotChocolate.Validation.Options;

namespace HotChocolate.Validation;

internal sealed class DefaultDocumentValidatorFactory : IDocumentValidatorFactory
{
    private readonly DocumentValidatorContextPool _contextPool;
    private readonly IValidationConfiguration _configuration;

    public DefaultDocumentValidatorFactory(
        DocumentValidatorContextPool contextPool,
        IValidationConfiguration configuration)
    {
        _contextPool = contextPool;
        _configuration = configuration;
    }

    public IDocumentValidator CreateValidator(string? schemaName = default)
    {
        schemaName ??= Schema.DefaultName;
        var options = _configuration.GetOptions(schemaName);
        var rules = _configuration.GetRules(schemaName);
        var aggregators = _configuration.GetResultAggregators(schemaName);
        return new DocumentValidator(_contextPool, rules, aggregators, options);
    }
}
