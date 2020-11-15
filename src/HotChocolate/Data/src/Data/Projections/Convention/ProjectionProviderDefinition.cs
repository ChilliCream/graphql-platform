using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public class ProjectionProviderDefinition : IHasScope
    {
        public string? Scope { get; set; }

        public IList<(Type, IProjectionFieldHandler?)> Handlers { get; } =
            new List<(Type, IProjectionFieldHandler?)>();

        public IList<(Type, IProjectionFieldInterceptor?)> Interceptors { get; } =
            new List<(Type, IProjectionFieldInterceptor?)>();

        public IList<(Type, IProjectionOptimizer?)> Optimizers { get; } =
            new List<(Type, IProjectionOptimizer?)>();
    }
}
