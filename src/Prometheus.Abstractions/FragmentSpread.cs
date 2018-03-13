using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Abstractions
{
    public class FragmentSpread
        : ISelection
    {
        private string _stringRepresentation;

        public FragmentSpread(string name, IEnumerable<Directive> directives)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Directives = directives == null
                ? new Dictionary<string, Directive>()
                : directives.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public string Name { get; }

        public IReadOnlyDictionary<string, Directive> Directives { get; }

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
            string s = $"{indentation}... {Name}";
            if (Directives.Any())
            {
                s = $"{s} {string.Join(" ", Directives.Values.Select(t => t.ToString()))}";
            }
            return s;
        }
    }
}