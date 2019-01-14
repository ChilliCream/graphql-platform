using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class DirectiveMiddlewareCompiler
    {
        private ILookup<FieldSelection, IDirective> _directives;
        private readonly ConcurrentDictionary<FieldSelection, FieldDelegate> _middlewareCache =
            new ConcurrentDictionary<FieldSelection, FieldDelegate>();

        public DirectiveMiddlewareCompiler(
            ILookup<FieldSelection, IDirective> directives)
        {
            if (directives == null)
            {
                throw new ArgumentNullException(nameof(directives));
            }

            _directives = directives;
        }

        public FieldDelegate GetOrCreateMiddleware(
            FieldSelection fieldSelection,
            FieldDelegate fieldPipeline)
        {
            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            if (fieldPipeline == null)
            {
                throw new ArgumentNullException(nameof(fieldPipeline));
            }

            if (!_middlewareCache.TryGetValue(
                fieldSelection,
                out FieldDelegate directivePipeline))
            {
                IEnumerable<IDirective> directives =
                    _directives[fieldSelection];
                if (directives.Any())
                {
                    directivePipeline = Compile(
                        fieldSelection, fieldPipeline,
                        directives);
                    _middlewareCache.TryAdd(fieldSelection, directivePipeline);
                }
            }

            return directivePipeline;
        }

        private FieldDelegate Compile(
            FieldSelection fieldSelection,
            FieldDelegate fieldPipeline,
            IEnumerable<IDirective> directives)
        {
            FieldDelegate next = fieldPipeline;

            foreach (IDirective directive in directives.Reverse())
            {
                next = BuildComponent(directive, next);
            }

            return next;
        }

        private FieldDelegate BuildComponent(
            IDirective directive,
            FieldDelegate next)
        {
            DirectiveDelegate component = directive.Middleware.Invoke(next);

            return context =>
            {
                if (HasErrors(context.Result))
                {
                    return Task.CompletedTask;
                }

                return component.Invoke(
                    new DirectiveContext(context, directive));
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
