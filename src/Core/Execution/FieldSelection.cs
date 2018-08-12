using System;
using System.Diagnostics;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    [DebuggerDisplay("{Field.Name}: {Field.Type}")]
    internal class FieldSelection
    {
        public FieldSelection(FieldNode node, ObjectField field, string responseName)
        {
            if (string.IsNullOrEmpty(responseName))
            {
                throw new ArgumentNullException(nameof(responseName));
            }

            Selection = node ?? throw new ArgumentNullException(nameof(node));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            ResponseName = responseName;
        }

        /// <summary>
        /// Gets the name the field will have in the query result.
        /// </summary>
        /// <value>
        /// Returns the name the field will have in the query result.
        /// </value>
        public string ResponseName { get; }

        /// <summary>
        /// Gets the selected field.
        /// </summary>
        /// <value>
        /// Returns the selected field.
        /// </value>
        public ObjectField Field { get; }

        /// <summary>
        /// Gets the field node which represents a field selection in a query.
        /// </summary>
        /// <value>
        /// Returns the field node which represents a field selection in a query.
        /// </value>
        public FieldNode Selection { get; }
    }
}
