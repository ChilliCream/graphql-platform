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

    public IDocumentValidator CreateValidator(NameString schemaName = default)
    {
        schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;
        ValidationOptions options = _configuration.GetOptions(schemaName);
        return new DocumentValidator(_contextPool, options.Rules, options);
    }
}
