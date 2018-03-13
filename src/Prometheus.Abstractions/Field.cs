using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prometheus.Abstractions
{
    public class Field
        : ISelection
    {
        private string _stringRepresentation;

        public Field(string name)
            : this(null, name, null, null, null)
        {
        }

        public Field(string name, IEnumerable<Argument> arguments)
            : this(null, name, arguments, null, null)
        {
        }

        public Field(string name, IEnumerable<Argument> arguments,
            IEnumerable<ISelection> selections)
            : this(null, name, arguments, null, selections)
        {
        }

        public Field(string alias, string name,
            IEnumerable<Argument> arguments,
            IEnumerable<Directive> directives,
            IEnumerable<ISelection> selections)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Alias = alias;
            Name = name;
            Arguments = arguments == null
                ? new Dictionary<string, Argument>()
                : arguments.ToDictionary(t => t.Name, StringComparer.Ordinal);
            Directives = directives == null
                ? new Dictionary<string, Directive>()
                : directives.ToDictionary(t => t.Name, StringComparer.Ordinal);
            SelectionSet = selections == null ? null : new SelectionSet(selections);
        }

        public string Name { get; }

        public string Alias { get; }

        public IReadOnlyDictionary<string, Argument> Arguments { get; }

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

            if (Alias != null)
            {
                sb.Append($"{Alias}: ");
            }

            sb.Append(Name);
            sb.Insert(0, indentation);

            if (Arguments.Any())
            {
                sb.Append($"({string.Join(", ", Arguments.Values.Select(t => t.ToString()))})");
            }

            if (Directives.Any())
            {
                sb.Append($" {string.Join(" ", Directives.Values.Select(t => t.ToString()))}");
            }

            if (SelectionSet != null && SelectionSet.Any())
            {
                sb.AppendLine();
                sb.Append(SelectionSet.ToString(indentationDepth));
            }

            return sb.ToString();
        }
    }
}