using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types
{
    public class SelectionMiddleware<T>
    {
        private readonly FieldDelegate _next;

        public SelectionMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }


        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            IQueryable<T> source = null;
            IReadOnlyList<IFieldSelection> selection;
            if (context.Result is PageableData<T> p)
            {
                // TODO add support for pagable data!
                throw new Exception("NOT SUPPORTED");
                source = p.Source;
            }
            else
            {
                if (context.Field.Type.NamedType() is ObjectType type)
                {
                    selection = context.CollectFields(type);
                }
                else
                {
                    throw new Exception("Illegal type");
                }
                p = null;
            }

            if (context.Result is IQueryable<T> q)
            {
                source = q;
            }
            else if (context.Result is IEnumerable<T> e)
            {
                source = e.AsQueryable();
            }

            IEnumerable<MemberInfo> members = selection.Select(x => x.Field.Member);
            Expression<Func<T, T>> projection = SelectionExpressionBuilder.Project<T>(members);
            context.Result = source.Select(projection);
        }
    }
}

