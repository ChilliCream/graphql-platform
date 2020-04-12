using System;
using HotChocolate.Validation.Options;
using HotChocolate.Validation.Properties;

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

        public IDocumentValidator CreateValidator(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    Resources.DefaultDocumentValidatorFactory_Schema_Name_Is_Mandatory,
                    nameof(schemaName));
            }

            return new DocumentValidator(_contextPool, _configuration.GetRules(schemaName));
        }
    }
}
