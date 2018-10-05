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
        private readonly IDictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>> _directiveLookup;

        public DirectiveLookup(
            IDictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>> directiveLookup)
        {
            _directiveLookup = directiveLookup
                ?? throw new ArgumentNullException(nameof(directiveLookup));
        }

        public IReadOnlyCollection<IDirective> GetDirectives(
            ObjectType type, FieldNode fieldSelection)
        {
            if (_directiveLookup.TryGetValue(
                    type, out var selectionToDirectives)
                && selectionToDirectives.TryGetValue(
                    fieldSelection, out var directives))
            {
                return directives;
            }
            return Array.Empty<IDirective>();
        }


        public Func<IResolverContext, object, Task<object>> GetMiddleware(
            ObjectType type, FieldNode fieldSelection)
        {
            // TODO : cache middleware
            return BuildMiddleware(type, fieldSelection);
        }

        private Func<IResolverContext, object, Task<object>> BuildMiddleware(
            ObjectType type, FieldNode fieldSelection)
        {
            Action<IDirectiveContext, IDirective> updateContext =
                (context, directive) =>
                {
                    if (context is DirectiveContext c)
                    {
                        c.Directive = directive;
                    }
                };

            DirectiveDelegate component = context =>
            {
                return Task.CompletedTask;
            };

            foreach (IDirective directive in
                GetDirectives(type, fieldSelection).Reverse())
            {
                component = BuildComponent(directive, updateContext, component);
            }

            return async (context, resolverResult) =>
            {
                DirectiveContext directiveContext = new DirectiveContext(context);
                directiveContext.Result = resolverResult;
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
                updateContext(context, directive);
                return component.Invoke(context);
            };
        }
    }
}
