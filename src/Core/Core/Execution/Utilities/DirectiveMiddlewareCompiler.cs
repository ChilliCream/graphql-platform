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
        private readonly ConcurrentDictionary<FieldSelection, FieldDelegate> _cache =
            new ConcurrentDictionary<FieldSelection, FieldDelegate>();
        private readonly ISchema _schema;

        public DirectiveMiddlewareCompiler(ISchema schema)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
        }

        public FieldDelegate GetOrCreateMiddleware(
            FieldSelection fieldSelection,
            Func<FieldDelegate> fieldPipeline)
        {
            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            if (!_cache.TryGetValue(fieldSelection,
                out FieldDelegate directivePipeline))
            {
                directivePipeline = fieldPipeline.Invoke();

                bool isIntrospectionField =
                    fieldSelection.Field.IsIntrospectionField
                    || fieldSelection.Field.DeclaringType.IsIntrospectionType();

                IReadOnlyCollection<IDirective> directives =
                    CollectDirectives(fieldSelection);

                if (!isIntrospectionField && directives.Any())
                {
                    directivePipeline = Compile(
                        fieldSelection, directivePipeline,
                        directives);
                }

                _cache.TryAdd(fieldSelection, directivePipeline);
            }

            return directivePipeline;
        }

        private IReadOnlyCollection<IDirective> CollectDirectives(
            FieldSelection fieldSelection)
        {
            HashSet<string> processed = new HashSet<string>();
            List<IDirective> directives = new List<IDirective>();

            CollectTypeSystemDirectives(
                processed, directives,
                fieldSelection.Field);

            CollectQueryDirectives(
                processed, directives,
                fieldSelection);

            return directives.AsReadOnly();
        }

        private void CollectQueryDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            FieldSelection fieldSelection)
        {
            foreach (IDirective directive in
                GetFieldSelectionDirectives(fieldSelection))
            {
                if (!directive.Type.IsRepeatable
                    && !processed.Add(directive.Name))
                {
                    directives.Remove(
                        directives.First(t => t.Type == directive.Type));
                }
                directives.Add(directive);
            }
        }

        private IEnumerable<IDirective> GetFieldSelectionDirectives(
            FieldSelection fieldSelection)
        {
            foreach (DirectiveNode directive in fieldSelection.Nodes
                .SelectMany(t => t.Directives))
            {
                if (_schema.TryGetDirectiveType(directive.Name.Value,
                    out DirectiveType directiveType))
                {
                    if (directiveType.IsExecutable)
                    {
                        yield return new Directive(
                            directiveType, directive, fieldSelection);
                    }
                }
            }
        }

        private static void CollectTypeSystemDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            ObjectField field)
        {
            foreach (IDirective directive in field.ExecutableDirectives)
            {
                if (!directive.Type.IsRepeatable
                    && !processed.Add(directive.Name))
                {
                    directives.Remove(
                        directives.First(t => t.Type == directive.Type));
                }
                directives.Add(directive);
            }
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
