using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class ProjectionConventionDefinition : IHasScope
    {
        public string? Scope { get; set; }

        public IList<(Type, IProjectionFieldHandler?)> Handlers { get; } =
            new List<(Type, IProjectionFieldHandler?)>();
    }
}
