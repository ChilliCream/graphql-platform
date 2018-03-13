using System;
using System.Collections.Generic;
using System.Text;

namespace Prometheus.Abstractions
{
    public class SelectionSet
        : ReadOnlySet<ISelection>
        , ISelectionSet
    {
        private string _stringRepresentation;

        public SelectionSet(IEnumerable<ISelection> items)
            : base(items)
        {
        }

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
            if (Count > 0)
            {
                string indentation = SerializationUtilities.Identation(indentationDepth);
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"{indentation}{{");
                foreach (ISelection selection in this)
                {
                    sb.AppendLine(selection.ToString(indentationDepth + 1));
                }
                sb.Append("}");

                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }

        }
    }
}