using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation.Options
{
    internal sealed class DefaultValidationBuilder : IValidationBuilder
    {
        public DefaultValidationBuilder(NameString name, IServiceCollection services)
        {
            Name = name;
            Services = services;
        }

        public NameString Name { get; }

        public IServiceCollection Services { get; }
    }
}
