using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prometheus.Abstractions
{
    public class InlineFragment
        : ISelection
        , IFragment
    {
        private string _stringRepresentation;

        public InlineFragment(NamedType typeCondition, IEnumerable<ISelection> selections)
            : this(typeCondition, selections, null)
        {
        }

        public InlineFragment(NamedType typeCondition, IEnumerable<ISelection> selections, IEnumerable<Directive> directives)
        {
            if (selections == null)
            {
                throw new ArgumentNullException(nameof(selections));
            }


            TypeCondition = typeCondition;
            SelectionSet = new SelectionSet(selections);
            Directives = directives == null
                ? new Dictionary<string, Directive>()
                : directives.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public NamedType TypeCondition { get; }

        public IReadOnlyDictionary<string, Directive> Directives { get; }

        public ISelectionSet SelectionSet { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                _stringRepresentation = ToString(0);
            }
            return _stringRepresentation;
        }

        public string ToString(int indentationDepth)
        {
            string indentation = SerializationUtilities.Identation(indentationDepth);
            StringBuilder sb = new StringBuilder();
            sb.Append($"{indentation}... on");

            if (TypeCondition != null)
            {
                sb.Append($" {TypeCondition.Name}");
            }

            if (Directives.Any())
            {
                sb.Append($" {string.Join(" ", Directives.Values.Select(t => t.ToString()))}");
            }

            sb.AppendLine();
            sb.Append(SelectionSet.ToString(indentationDepth));

            return sb.ToString();
        }
    }
}