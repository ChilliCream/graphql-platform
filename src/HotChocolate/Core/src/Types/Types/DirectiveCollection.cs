using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Utilities.ErrorHelper;

namespace HotChocolate.Types
{
    public sealed class DirectiveCollection : IDirectiveCollection
    {
        private readonly object _source;
        private readonly List<IDirective> _directives = new();
        private readonly DirectiveLocation _location;
        private List<DirectiveDefinition> _definitions;
        private ILookup<NameString, IDirective> _lookup;

        public DirectiveCollection(
            object source,
            IEnumerable<DirectiveDefinition> directiveDefinitions)
        {
            if (directiveDefinitions is null)
            {
                throw new ArgumentNullException(nameof(directiveDefinitions));
            }

            _source = source ?? throw new ArgumentNullException(nameof(source));
            _definitions = directiveDefinitions.ToList();
            _location = DirectiveHelper.InferDirectiveLocation(source);
        }

        public int Count => _directives.Count;

        public IEnumerable<IDirective> this[NameString key] => _lookup[key];

        public bool Contains(NameString key) => _lookup.Contains(key);

        public void CompleteCollection(ITypeCompletionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var processed = new HashSet<string>();

            foreach (DirectiveDefinition description in _definitions)
            {
                if (TryCompleteDirective(
                    context, description, processed,
                    out Directive directive))
                {
                    _directives.Add(directive);
                    ValidateArguments(context, directive);
                }
            }

            _lookup = _directives.ToLookup(t => t.Name);
            _definitions = null;
        }

        private bool TryCompleteDirective(
            ITypeCompletionContext context,
            DirectiveDefinition definition,
            ISet<string> processed,
            out Directive directive)
        {
            if (!context.TryGetDirectiveType(definition.Reference, out DirectiveType directiveType))
            {
                directive = null;
                return false;
            }

            if (!processed.Add(directiveType.Name) && !directiveType.IsRepeatable)
            {
                context.ReportError(
                    DirectiveCollection_DirectiveIsUnique(
                        directiveType, context.Type,
                        definition.ParsedDirective, _source));
                directive = null;
                return false;
            }

            if (directiveType.Locations.Contains(_location))
            {
                directive = Directive.FromDescription(directiveType, definition, _source);
                return true;
            }

            context.ReportError(
                DirectiveCollection_LocationNotAllowed(
                    directiveType, _location, context.Type,
                    definition.ParsedDirective, _source));
            directive = null;
            return false;
        }

        private void ValidateArguments(ITypeCompletionContext context, Directive directive)
        {
            var arguments = directive.ToNode().Arguments.ToDictionary(t => t.Name.Value);

            foreach (ArgumentNode argument in arguments.Values)
            {
                if (directive.Type.Arguments.TryGetField(
                    argument.Name.Value, out Argument arg))
                {
                    if (!arg.Type.IsInstanceOfType(argument.Value))
                    {
                        context.ReportError(
                            DirectiveCollection_ArgumentValueTypeIsWrong(
                                directive.Type, context.Type, directive.ToNode(),
                                _source, arg.Name));
                    }
                }
                else
                {
                    context.ReportError(
                        DirectiveCollection_ArgumentDoesNotExist(
                            directive.Type, context.Type, directive.ToNode(),
                            _source, argument.Name.Value));
                }
            }

            foreach (Argument argument in directive.Type.Arguments
                .Where(a => a.Type.IsNonNullType()))
            {
                if (!arguments.TryGetValue(argument.Name, out ArgumentNode arg)
                    || arg.Value is NullValueNode)
                {
                    context.ReportError(
                        DirectiveCollection_ArgumentNonNullViolation(
                            directive.Type, context.Type, directive.ToNode(),
                            _source, argument.Name));
                }
            }
        }
        public IEnumerator<IDirective> GetEnumerator() =>
            _directives.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public static DirectiveCollection CreateAndComplete(
            ITypeCompletionContext context,
            object source,
            IEnumerable<DirectiveDefinition> directiveDefinitions)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (directiveDefinitions is null)
            {
                throw new ArgumentNullException(nameof(directiveDefinitions));
            }

            var directives = new DirectiveCollection(source, directiveDefinitions);
            directives.CompleteCollection(context);
            return directives;
        }
    }
}
