using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddleware
    {
        private readonly FieldDelegate _next;

        public DelegateToRemoteSchemaMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            IDirective directive = context.Field
                .Directives[DirectiveNames.Delegate]
                .FirstOrDefault();

            IDirective schema = context.Field
                .Directives[DirectiveNames.Schema]
                .FirstOrDefault();

            if (directive != null && schema != null)
            {
                var stitchingContext = context.Service<IStitchingContext>();
                string schemaName = schema.ToObject<SchemaDirective>().Name;

                IQueryExecutor remoteExecutor =
                    stitchingContext.GetQueryExecutor(schemaName);




                // fetch data from remote schema
            }

            await _next.Invoke(context);
        }


        private static IReadOnlyDictionary<string, object> CreateVariables(
            IDirectiveContext directiveContext,
            IEnumerable<SelectionPathComponent> components)
        {
            var root = new Dictionary<string, object>();

            foreach (var component in components)
            {
                foreach (ArgumentNode argument in component.Arguments)
                {
                    if (argument.Value is ScopedVariableNode sv)
                    {
                        switch (sv.Scope.Value)
                        {
                            case "arguments":
                                root[sv.ToVariableName()] =
                                    directiveContext.Argument<object>(
                                        sv.Name.Value);
                                break;
                            case "variables":
                                break;
                            case "properties":
                                root[sv.ToVariableName()] = directiveContext
                                    .Parent<IDictionary<string, object>>()
                                        [sv.Name.Value];
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }

            return root;
        }
    }
}
