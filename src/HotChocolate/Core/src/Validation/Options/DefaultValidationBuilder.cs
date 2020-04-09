using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation.Options
{
    internal sealed class DefaultValidationBuilder : IValidationBuilder
    {
        public DefaultValidationBuilder(string name, IServiceCollection services)
        {
            Name = name;
            Services = services;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
