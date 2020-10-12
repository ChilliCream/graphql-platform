using System;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class ProjectionConventionDefinition : IHasScope
    {
        public string? Scope { get; set; }

        public Type? Provider { get; set; }

        public IProjectionProvider? ProviderInstance { get; set; }
    }
}
