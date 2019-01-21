using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddleware
    {
        private readonly FieldDelegate _next;
        private static readonly NameString _delegateName = "delegate";

        public DelegateToRemoteSchemaMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            IDirective directive = context.Field.Directives[_delegateName]
                .FirstOrDefault();

            if (directive != null)
            {
                // fetch data from remote schema
            }

            await _next.Invoke(context);
        }
    }
}
