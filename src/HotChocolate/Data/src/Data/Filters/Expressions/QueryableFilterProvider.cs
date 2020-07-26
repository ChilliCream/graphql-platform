using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterContext : FilterVisitorContext<Expression>
    {
        public QueryableFilterContext(
            IFilterInputType initialType,
            bool inMemory)
            : base(initialType,
                  new QueryableScope(initialType.EntityType, "_s0", inMemory))
        {
            InMemory = inMemory;
            ClrTypes = new Stack<Type>();
            ClrTypes.Push(initialType.EntityType);
        }

        public bool InMemory { get; }

        public Stack<Type> ClrTypes { get; }

        public override FilterScope<Expression> CreateScope() =>
             new QueryableScope(ClrTypes.Peek(), "_s" + Scopes.Count, InMemory);
    }

    public class QueryableFilterProvider
        : FilterProvider<Expression, QueryableFilterContext>
    {
        public override async Task ExecuteAsync<TEntityType>(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            await next(context).ConfigureAwait(false);

            string argumentName = Convention!.GetArgumentName();

            IValueNode filter = context.Argument<IValueNode>(argumentName);

            if (filter is null || filter is NullValueNode)
            {
                return;
            }

            IQueryable<TEntityType>? source = null;
            PageableData<TEntityType>? p = null;
            if (context.Result is PageableData<TEntityType> pd)
            {
                source = pd.Source;
                p = pd;
            }

            if (context.Result is IQueryable<TEntityType> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<TEntityType> e)
            {
                source = e.AsQueryable();
            }

            if (source != null
                && context.Field.Arguments[argumentName].Type is InputObjectType iot
                && iot is IFilterInputType fit)
            {
                var visitorContext = new QueryableFilterContext(
                    fit, source is EnumerableQuery);
                Visitor.Visit(filter, visitorContext);

                if (visitorContext.TryCreateLambda<TEntityType>(
                        out Expression<Func<TEntityType, bool>>? where))
                {
                    source = source.Where(where);

                    context.Result = p is null
                        ? (object)source
                        : new PageableData<TEntityType>(source, p.Properties);
                }
                else
                {
                    if (visitorContext.Errors.Count > 0)
                    {
                        context.Result = Array.Empty<TEntityType>();
                        foreach (IError error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
                        }
                    }
                }
            }
        }
    }

    public class QueryableScope
        : FilterScope<Expression>
    {
        public QueryableScope(
            Type type,
            string parameterName,
            bool inMemory)
        {
            Parameter = Expression.Parameter(type, parameterName);
            InMemory = inMemory;
            Instance.Push(Parameter);
        }

        public ParameterExpression Parameter { get; }

        public bool InMemory { get; }

    }
}