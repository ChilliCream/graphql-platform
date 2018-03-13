using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Abstractions
{
    public class Directive
    {
        private string _stringRepresentation;

        public Directive(string name, IEnumerable<Argument> arguments)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Name = name;
            Arguments = arguments.ToDictionary(t => t.Name, StringComparer.Ordinal);
        }

        public string Name { get; }

        public IReadOnlyDictionary<string, Argument> Arguments { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                _stringRepresentation = $"@{Name}({string.Join(", ", Arguments.Select(t => t.ToString()))})";
            }
            return _stringRepresentation;
        }

    }
}