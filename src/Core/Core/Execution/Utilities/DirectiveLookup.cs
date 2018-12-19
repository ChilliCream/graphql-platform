using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class DirectiveLookup
    {
        private readonly Dictionary<ObjectType, Dictionary<FieldNode, ExecuteMiddleware>> _middlewareLookup =
            new Dictionary<ObjectType, Dictionary<FieldNode, ExecuteMiddleware>>();

        public DirectiveLookup(IDictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>> directiveLookup)
        {
            if (directiveLookup == null)
            {
                throw new ArgumentNullException(nameof(directiveLookup));
            }

            Build(directiveLookup);
        }

        public ExecuteMiddleware GetMiddleware(
            ObjectType type,
            FieldNode fieldSelection)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            if (_middlewareLookup.TryGetValue(
                    type, out var selectionToDirectives)
                && selectionToDirectives.TryGetValue(
                    fieldSelection, out var middleware))
            {
                return middleware;
            }

            return null;
        }

        private void Build(IDictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>> directiveLookup)
        {
            foreach (var objectType in directiveLookup)
            {
                var middlewareLookup =
                    new Dictionary<FieldNode, ExecuteMiddleware>();

                foreach (var fieldNode in objectType.Value)
                {
                    if (fieldNode.Value.Count > 0)
                    {
                        ExecuteMiddleware middleware =
                            BuildMiddleware(fieldNode.Value);
                        middlewareLookup[fieldNode.Key] = middleware;
                    }
                }

                _middlewareLookup[objectType.Key] = middlewareLookup;
            }
        }

        private ExecuteMiddleware BuildMiddleware(
            IEnumerable<IDirective> directives)
        {
            Action<IDirectiveContext, IDirective> updateContext =
                (context, directive) =>
                {
                    if (context is DirectiveContext c)
                    {
                        c.Directive = directive;
                    }
                };

            DirectiveDelegate component = async context =>
            {
                if (!context.IsResultModified)
                {
                    context.Result = await context.ResolveAsync<object>();
                }
            };

            HashSet<string> processed = new HashSet<string>();
            foreach (IDirective directive in directives.Reverse())
            {
                if (processed.Add(directive.Name))
                {
                    component = BuildComponent(directive, updateContext, component);
                }
            }

            return async (context, executeResolver) =>
            {
                var directiveContext = new DirectiveContext(
                    context, executeResolver);
                await component.Invoke(directiveContext);
                return directiveContext.Result;
            };
        }

        private DirectiveDelegate BuildComponent(
            IDirective directive,
            Action<IDirectiveContext, IDirective> updateContext,
            DirectiveDelegate next)
        {
            DirectiveDelegate component = directive.Middleware.Invoke(next);

            return context =>
            {
                if (HasErrors(context.Result))
                {
                    return Task.CompletedTask;
                }

                updateContext(context, directive);
                return component.Invoke(context)
                    .ContinueWith((task, state) =>
                        updateContext(context, directive), null);
            };
        }

        private bool HasErrors(object result)
        {
            if (result is IError error
                || result is IEnumerable<IError> errors)
            {
                return true;
            }

            return false;
        }
    }
}
