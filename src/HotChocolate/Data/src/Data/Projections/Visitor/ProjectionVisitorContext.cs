using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections
{
    public abstract class ProjectionVisitorContext<T>
        : SelectionVisitorContext,
          IProjectionVisitorContext<T>
    {
        protected ProjectionVisitorContext(
            IResolverContext context,
            IOutputType initialType,
            ProjectionScope<T> projectionScope) : base(context)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }

            Types.Push(initialType);
            Scopes = new Stack<ProjectionScope<T>>();
            Scopes.Push(projectionScope);
        }

        public Stack<ProjectionScope<T>> Scopes { get; }

        public Stack<IType> Types { get; } = new Stack<IType>();

        public IList<IError> Errors { get; } = new List<IError>();
    }
}
