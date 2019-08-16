using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
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

        public bool Contains(NameString key) => _lookup.Contains(key);

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
            ICompletionContext context,
            DirectiveDefinition definition,
            ISet<string> processed,
            out Directive directive)
        {
            DirectiveType directiveType =
                context.GetDirectiveType(definition.Reference);
            directive = null;

            if (directiveType != null)
            {
                if (!processed.Add(directiveType.Name)
                    && !directiveType.IsRepeatable)
                {
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeResources.DirectiveCollection_DirectiveIsUnique,
                            directiveType.Name))
                        .SetCode(ErrorCodes.Schema.MissingType)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(definition.ParsedDirective)
                        .Build());
                }
                else if (directiveType.Locations.Contains(_location))
                {
                    directive = Directive.FromDescription(directiveType, definition, _source);
                }
                else
                {
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeResources.DirectiveCollection_LocationNotAllowed,
                            directiveType.Name,
                            _location))
                        .SetCode(ErrorCodes.Schema.MissingType)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(definition.ParsedDirective)
                        .Build());
                }
            }

            return directive != null;
        }

        private void ValidateArguments(ICompletionContext context, Directive directive)
        {
            Dictionary<string, ArgumentNode> arguments =
                directive.ToNode().Arguments.ToDictionary(t => t.Name.Value);

            foreach (ArgumentNode argument in arguments.Values)
            {
                if (directive.Type.Arguments.TryGetField(
                    argument.Name.Value, out Argument arg))
                {
                    if (!arg.Type.IsInstanceOfType(argument.Value))
                    {
                        // TODO : resources
                        context.ReportError(SchemaErrorBuilder.New()
                            .SetMessage(string.Format(
                                CultureInfo.InvariantCulture,
                                "The argument `{0}` value type is wrong.",
                                arg.Name))
                            .SetCode(ErrorCodes.Schema.ArgumentValueTypeWrong)
                            .SetTypeSystemObject(context.Type)
                            .AddSyntaxNode(directive.ToNode())
                            .Build());
                    }
                }
                else
                {
                    // TODO : resources
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            "The argument `{0}` does not exist on the " +
                            "directive `{1}`.",
                            argument.Name.Value,
                            directive.Type.Name))
                        .SetCode(ErrorCodes.Schema.InvalidArgument)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(directive.ToNode())
                        .Build());
                }
            }

            foreach (Argument argument in directive.Type.Arguments
                .Where(a => a.Type.IsNonNullType()))
            {
                if (!arguments.TryGetValue(argument.Name, out ArgumentNode arg)
                    || arg.Value is NullValueNode)
                {
                    // TODO : resources
                    context.ReportError(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            "The argument `{0}` of directive `{1}` " +
                            "mustn't be null.",
                            argument.Name.Value,
                            directive.Type.Name))
                        .SetCode(ErrorCodes.Schema.NonNullArgument)
                        .SetTypeSystemObject(context.Type)
                        .AddSyntaxNode(directive.ToNode())
                        .Build());
                }
            }
        }

        #endregion

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
