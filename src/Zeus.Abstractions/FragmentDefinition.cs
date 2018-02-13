using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeus.Abstractions
{
    public class FragmentDefinition
        : IQueryDefinition
        , IFragment
    {
        private string _stringRepresentation;

        public FragmentDefinition(string name, NamedType typeCondition,
            IEnumerable<ISelection> selections)
             : this(name, typeCondition, selections, null)
        {
        }

        public FragmentDefinition(string name, NamedType typeCondition,
            IEnumerable<ISelection> selections, IEnumerable<Directive> directives)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (typeCondition == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (selections == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            TypeCondition = typeCondition;
            SelectionSet = new SelectionSet(selections);
            Directives = directives == null
                ? new Dictionary<string, Directive>()
                : directives.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public string Name { get; }

        public NamedType TypeCondition { get; }

        public IReadOnlyDictionary<string, Directive> Directives { get; }

        public ISelectionSet SelectionSet { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"fragment {Name} on {TypeCondition.Name}");

                if (Directives.Any())
                {
                    sb.Append($" {string.Join(" ", Directives.Values.Select(t => t.ToString()))}");
                }

                sb.AppendLine();
                sb.AppendLine("{");
                sb.AppendLine(SelectionSet.ToString());
                sb.Append("}");

                _stringRepresentation = sb.ToString();
            }
            return _stringRepresentation;
        }
    }
}