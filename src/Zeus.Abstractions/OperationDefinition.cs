using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeus.Abstractions
{
    public class OperationDefinition
        : IQueryDefinition
    {
        private string _stringRepresentation;

        public OperationDefinition(string name, OperationType type,
            IEnumerable<ISelection> selections)
            : this(name, type, null, selections)
        {
        }

        public OperationDefinition(string name, OperationType type,
            IEnumerable<VariableDefinition> variableDefinitions,
            IEnumerable<ISelection> selections)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (selections == null)
            {
                throw new ArgumentNullException(nameof(selections));
            }

            Name = name;
            Type = type;
            VariableDefinitions = variableDefinitions == null
                ? new Dictionary<string, VariableDefinition>()
                : variableDefinitions.ToDictionary(t => t.Name, StringComparer.Ordinal);
            SelectionSet = new SelectionSet(selections);
        }

        public string Name { get; }

        public OperationType Type { get; }

        public IReadOnlyDictionary<string, VariableDefinition> VariableDefinitions { get; }

        public ISelectionSet SelectionSet { get; }
    
        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                StringBuilder sb = new StringBuilder();
                if (Name != null)
                {
                    sb.Append($"{Type} {Name}");
                    if (VariableDefinitions.Any())
                    {
                        sb.Append($"({string.Join(", ", VariableDefinitions.Values.Select(t => t.ToString()))})");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("{");
                foreach (ISelection selection in SelectionSet)
                {
                    sb.AppendLine($"  {selection}");
                }
                sb.Append("}");

                _stringRepresentation = sb.ToString();
            }
            return _stringRepresentation;
        }
    }
}