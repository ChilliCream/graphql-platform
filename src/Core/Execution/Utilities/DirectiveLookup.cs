using System;
using System.Collections.Generic;
using HotChocolate.Language;
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
    }
}
