using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zeus.Abstractions
{
    public class FieldDefinition
        : IFieldDefinition
    {
        private string _stringRepresentation;

        public FieldDefinition(string name, IType type)
            : this(name, type, false, null)
        {
        }

        public FieldDefinition(
            string name, IType type,
            bool isIntrospectionField)
            : this(name, type, isIntrospectionField, null)
        {
        }

        public FieldDefinition(
            string name, IType type,
            IEnumerable<InputValueDefinition> arguments)
            : this(name, type, false, arguments)
        {
        }

        public FieldDefinition(
            string name, IType type,
            bool isIntrospectionField,
            IEnumerable<InputValueDefinition> arguments)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "A type definition name must not be null or empty.",
                    nameof(name));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Name = name;
            Type = type;
            Arguments = arguments == null
                ? new Dictionary<string, InputValueDefinition>()
                : arguments.ToDictionary(t => t.Name, StringComparer.Ordinal);
            IsIntrospectionField = isIntrospectionField;
        }

        public string Name { get; }

        public IType Type { get; }

        public IReadOnlyDictionary<string, InputValueDefinition> Arguments { get; }

        public bool IsIntrospectionField { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(Name);

                if (Arguments.Any())
                {
                    sb.Append("(");
                    sb.Append(string.Join(", ", Arguments.Select(t => t.Value.ToString())));
                    sb.Append(")");
                }

                sb.Append($": {Type}");

                _stringRepresentation = sb.ToString();
            }

            return _stringRepresentation;
        }
    }
}