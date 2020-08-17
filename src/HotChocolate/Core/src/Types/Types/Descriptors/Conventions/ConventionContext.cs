using System;

#nullable enable
namespace HotChocolate.Types.Descriptors
{
    public class ConventionContext : IConventionContext
    {
        public ConventionContext(
            string? scope,
            IServiceProvider services)
        {
            Scope = scope;
            Services = services;
        }

        public string? Scope { get; }

        public IServiceProvider Services { get; }
    }
}
