using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    internal sealed class DirectiveCollection
        : IDirectiveCollection
    {
        private readonly object _source;
        private readonly List<IDirective> _directives = new List<IDirective>();
        private readonly DirectiveLocation _location;
        private List<DirectiveDefinition> _definitions;
        private ILookup<NameString, IDirective> _lookup;

        public DirectiveCollection(
            object source,
            IEnumerable<DirectiveDefinition> directiveDefinitions)
        {
            if (directiveDefinitions == null)
            {
                throw new ArgumentNullException(nameof(directiveDefinitions));
            }

            _source = source
                ?? throw new ArgumentNullException(nameof(source));
            _definitions = directiveDefinitions.ToList();
            _location = DirectiveHelper.InferDirectiveLocation(source);
        }

        public int Count => _directives.Count;

        public IEnumerable<IDirective> this[NameString key] => _lookup[key];

        #region Initialization

        internal void CompleteCollection(ICompletionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var processed = new HashSet<string>();

            foreach (DirectiveDefinition description in _definitions)
            {
                CompleteDirective(context, description, processed);
            }

            _lookup = _directives.ToLookup(t => t.Name);
            _definitions = null;
        }

        private void CompleteDirective(
            ICompletionContext context,
            DirectiveDefinition definition,
            ISet<string> processed)
        {
            DirectiveType directiveType =
                context.GetDirectiveType(definition.Reference);

            if (directiveType != null)
            {
                if (!processed.Add(directiveType.Name)
                    && !directiveType.IsRepeatable)
                {
                    // TODO : resources
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(
                            $"The specified directive `@{directiveType.Name}` " +
                            "is unique and cannot be added twice.")
                        .SetCode(TypeErrorCodes.MissingType)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(definition.ParsedDirective)
                        .Build());
                }
                else if (directiveType.Locations.Contains(_location))
                {
                    _directives.Add(Directive.FromDescription(
                        directiveType, definition, _source));
                }
                else
                {
                    // TODO : resources
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(
                            $"The specified directive `@{directiveType.Name}` " +
                            "is not allowed on the current location " +
                            $"`{_location}`.")
                        .SetCode(TypeErrorCodes.MissingType)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(definition.ParsedDirective)
                        .Build());
                }
            }
        }

        #endregion

        public bool Contains(NameString key) => _lookup.Contains(key);

        public IEnumerator<IDirective> GetEnumerator()
        {
            return _directives.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
