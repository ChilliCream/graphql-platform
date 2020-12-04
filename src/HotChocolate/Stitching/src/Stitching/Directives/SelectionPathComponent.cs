using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Stitching
{
    public class SelectionPathComponent
    {
        public SelectionPathComponent(
            NameNode name,
            IReadOnlyList<ArgumentNode> arguments)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
        }

        public NameNode Name { get; }

        public IReadOnlyList<ArgumentNode> Arguments { get; }

        public override string ToString()
        {
            if (Arguments.Count == 0)
            {
                return Name.Value;
            }

            var sb = new StringBuilder();
            sb.Append(Name.Value);
            sb.Append('(');
            sb.Append(SerializeArgument(Arguments[0]));

            for (int i = 1; i < Arguments.Count; i++)
            {
                sb.Append(',');
                sb.Append(' ');
                sb.Append(SerializeArgument(Arguments[i]));
            }

            sb.Append(')');
            return sb.ToString();
        }

        private static string SerializeArgument(ArgumentNode argument)
        {
            return $"{argument.Name.Value}: {SerializeValue(argument.Value)}";
        }

        private static string SerializeValue(IValueNode value)
        {
            if (value is ScopedVariableNode variable)
            {
                return $"${variable.Scope.Value}:{variable.Name.Value}";
            }
            return value.Print();
        }
    }
}
