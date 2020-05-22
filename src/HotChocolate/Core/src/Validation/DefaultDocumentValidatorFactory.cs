using HotChocolate.Validation.Options;

namespace HotChocolate.Validation
{
    internal sealed class DefaultDocumentValidatorFactory
        : IDocumentValidatorFactory
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

        public IDocumentValidator CreateValidator(string? schemaName = null)
        {            
            return new DocumentValidator(
                _contextPool, 
                _configuration.GetRules(schemaName ??  
                    Microsoft.Extensions.Options.Options.DefaultName));
        }
    }
}
