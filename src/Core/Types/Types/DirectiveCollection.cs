using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types
{
    internal sealed class DirectiveCollection
        : TypeSystemBase
        , IDirectiveCollection
    {
        private readonly List<IDirective> _directives = new List<IDirective>();
        private readonly TypeSystemBase _source;
        private readonly DirectiveLocation _location;
        private readonly IReadOnlyCollection<DirectiveDescription> _descriptions;

        internal DirectiveCollection(
            TypeSystemBase source,
            DirectiveLocation location,
            IReadOnlyCollection<DirectiveDescription> directiveDescriptions)
        {
            _source = source;
            _location = location;
            _descriptions = directiveDescriptions
                ?? throw new ArgumentNullException(
                        nameof(directiveDescriptions));
        }

        public int Count => _directives.Count;

        #region Initialization

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            foreach (DirectiveDescription description in _descriptions)
            {
                CompleteDirecive(context, description);
            }
        }

        private void CompleteDirecive(
            ITypeInitializationContext context,
            DirectiveDescription description)
        {
            DirectiveReference reference =
                DirectiveReference.FromDescription(description);
            DirectiveType type = context.GetDirectiveType(reference);

            if (type != null)
            {
                if (type.Locations.Contains(_location))
                {
                    _directives.Add(
                        Directive.FromDescription(type, description, _source));
                }
                else
                {
                    context.ReportError(new SchemaError(
                        $"The specified directive `{type.Name}` is not " +
                        $"allowed on the current location `{_location}`.",
                        context.Type));
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
