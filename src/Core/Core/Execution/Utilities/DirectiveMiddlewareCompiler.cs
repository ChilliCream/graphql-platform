using System;
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
        private readonly ISchema _schema;

        public DirectiveMiddlewareCompiler(ISchema schema)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
        }

        public FieldDelegate GetOrCreateMiddleware(
            ObjectField field,
            FieldNode selection,
            Func<FieldDelegate> fieldPipeline)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (fieldPipeline == null)
            {
                throw new ArgumentNullException(nameof(fieldPipeline));
            }

            FieldDelegate directivePipeline = fieldPipeline.Invoke();

            if (field.ExecutableDirectives.Count > 0
                || selection.Directives.Count > 0)
            {
                IReadOnlyList<IDirective> directives =
                    CollectDirectives(field, selection);

                if (directives.Any())
                {
                    directivePipeline = Compile(
                        directivePipeline,
                        directives);
                }
            }

            return directivePipeline;
        }

        private IReadOnlyList<IDirective> CollectDirectives(
            ObjectField field,
            FieldNode selection)
        {
            var processed = new HashSet<string>();
            var directives = new List<IDirective>();

            CollectTypeSystemDirectives(
                processed,
                directives,
                field);

            CollectQueryDirectives(
                processed,
                directives,
                field,
                selection);

            return directives.AsReadOnly();
        }

        private void CollectQueryDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            ObjectField field,
            FieldNode selection)
        {
            foreach (IDirective directive in
                GetFieldSelectionDirectives(field, selection))
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
            ObjectField field,
            FieldNode selection)
        {
            foreach (DirectiveNode directive in selection.Directives)
            {
                if (_schema.TryGetDirectiveType(directive.Name.Value,
                    out DirectiveType directiveType)
                    && directiveType.IsExecutable)
                {
                    yield return new Directive(directiveType, directive, field);
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

        private static FieldDelegate Compile(
            FieldDelegate fieldPipeline,
            IReadOnlyList<IDirective> directives)
        {
            FieldDelegate next = fieldPipeline;

            for (int i = directives.Count - 1; i >= 0; i--)
            {
                next = BuildComponent(directives[i], next);
            }

            return next;
        }

        private static FieldDelegate BuildComponent(
            IDirective directive,
            FieldDelegate first)
        {
            FieldDelegate next = first;

            IReadOnlyList<DirectiveMiddleware> components =
                directive.MiddlewareComponents;

            for (int i = components.Count - 1; i >= 0; i--)
            {
                DirectiveDelegate component = components[i].Invoke(next);

                next = context =>
                {
                    if (HasErrors(context.Result))
                    {
                        return Task.CompletedTask;
                    }

                    return component.Invoke(
                        new DirectiveContext(context, directive));
                };
            }

            return next;
        }

        private static bool HasErrors(object result)
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
