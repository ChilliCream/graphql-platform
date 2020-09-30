using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Projections
{
    public abstract class ProjectionVisitorContext<T>
        : SelectionVisitorContext,
          IProjectionVisitorContext<T>
    {
        protected ProjectionVisitorContext(
            IResolverContext context,
            IOutputType initialType,
            ProjectionScope<T>? filterScope = null) : base(context)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }

            Types.Push(initialType);
            Scopes = new Stack<ProjectionScope<T>>();
            Scopes.Push(filterScope ?? CreateScope());
        }

        public Stack<ProjectionScope<T>> Scopes { get; }

        public Stack<IType> Types { get; } = new Stack<IType>();

        public Stack<IOutputField> Operations { get; } = new Stack<IOutputField>();

        public IList<IError> Errors { get; } = new List<IError>();

        public virtual ProjectionScope<T> CreateScope()
        {
            return new ProjectionScope<T>();
        }
    }
}
