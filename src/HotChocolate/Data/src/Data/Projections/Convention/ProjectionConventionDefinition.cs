using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class ProjectionConventionDefinition : IHasScope
    {
        public string? Scope { get; set; }

        public Type? Provider { get; set; }

        public IProjectionProvider? ProviderInstance { get; set; }

        public List<IProjectionProviderExtension> ProviderExtensions { get; } =
            new List<IProjectionProviderExtension>();

        public List<Type> ProviderExtensionsTypes { get; } = new List<Type>();
    }
}
