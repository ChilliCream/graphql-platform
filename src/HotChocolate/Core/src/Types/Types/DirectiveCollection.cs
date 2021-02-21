using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable
namespace HotChocolate.Types
{
    public sealed class DirectiveCollection : IDirectiveCollection
    {
        private static ILookup<NameString, IDirective> _defaultLookup =
            Enumerable.Empty<IDirective>().ToLookup(x => x.Name);

        private readonly object _source;
        private readonly DirectiveLocation _location;
        private IDirective[] _directives = Array.Empty<IDirective>();
        private DirectiveDefinition[]? _definitions;
        private ILookup<NameString, IDirective> _lookup = default!;

        public DirectiveCollection(
            object source,
            IEnumerable<DirectiveDefinition> directiveDefinitions)
        {
            if (directiveDefinitions is null)
            {
                throw new ArgumentNullException(nameof(directiveDefinitions));
            }

            _source = source ?? throw new ArgumentNullException(nameof(source));
            _definitions = directiveDefinitions.Any()
                ? directiveDefinitions.ToArray()
                : Array.Empty<DirectiveDefinition>();
            _location = DirectiveHelper.InferDirectiveLocation(source);
        }

        public int Count => _directives.Length;

        public IEnumerable<IDirective> this[NameString key] => _lookup[key];

        public bool Contains(NameString key) => _lookup.Contains(key);

        public void CompleteCollection(ITypeCompletionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var processed = new HashSet<string>();
            List<IDirective>? directives = null;

            foreach (DirectiveDefinition description in _definitions)
            {
                if (TryCompleteDirective(
                    context,
                    description,
                    processed,
                    out Directive directive))
                {
                    directives ??= new List<IDirective>();
                    directives.Add(directive);
                    ValidateArguments(context, directive);
                }
            }

            if (directives is null)
            {
                _lookup = _defaultLookup;
            }
            else
            {
                _directives = directives.ToArray();
                _lookup = _directives.ToLookup(t => t.Name);
            }

            _definitions = null;
        }

        private bool TryCompleteDirective(
            ITypeCompletionContext context,
            DirectiveDefinition definition,
            ISet<string> processed,
            [NotNullWhen(true)] out Directive? directive)
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
                        directiveType,
                        context.Type,
                        definition.ParsedDirective,
                        _source));
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
                    directiveType,
                    _location,
                    context.Type,
                    definition.ParsedDirective,
                    _source));
            directive = null;
            return false;
        }

        private void ValidateArguments(ITypeCompletionContext context, Directive directive)
        {
            var arguments = directive.ToNode().Arguments.ToDictionary(t => t.Name.Value);

            foreach (ArgumentNode argument in arguments.Values)
            {
                if (directive.Type.Arguments.TryGetField(
                    argument.Name.Value,
                    out Argument arg))
                {
                    if (!arg.Type.IsInstanceOfType(argument.Value))
                    {
                        context.ReportError(
                            DirectiveCollection_ArgumentValueTypeIsWrong(
                                directive.Type,
                                context.Type,
                                directive.ToNode(),
                                _source,
                                arg.Name));
                    }
                }
                else
                {
                    context.ReportError(
                        DirectiveCollection_ArgumentDoesNotExist(
                            directive.Type,
                            context.Type,
                            directive.ToNode(),
                            _source,
                            argument.Name.Value));
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
                            directive.Type,
                            context.Type,
                            directive.ToNode(),
                            _source,
                            argument.Name));
                }
            }
        }

        public IEnumerator<IDirective> GetEnumerator()
        {
            for (var i = 0; i < _directives.Length; i++)
            {
                yield return _directives[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public static IDirectiveCollection CreateAndComplete(
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

            if (!directiveDefinitions.Any())
            {
                return EmptyDirectiveCollection.Default;
            }

            var directives = new DirectiveCollection(source, directiveDefinitions);
            directives.CompleteCollection(context);
            return directives;
        }

        internal class EmptyDirectiveCollection : IDirectiveCollection
        {
            private EmptyDirectiveCollection() { }

            public IEnumerator<IDirective> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => 0;

            public IEnumerable<IDirective> this[NameString key] => Array.Empty<IDirective>();


            public bool Contains(NameString key) => false;


            public static readonly IDirectiveCollection Default = new EmptyDirectiveCollection();
        }
    }
}
