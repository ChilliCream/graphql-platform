using System;
using System.Diagnostics;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// Represents a query field selection and provides access to the
    /// <see cref="FieldNode"/> and actual <see cref="ObjectField"/>
    /// to which the <see cref="FieldNode"/> referrs to.
    /// </summary>
    [DebuggerDisplay("{Field.Name}: {Field.Type}")]
    public class FieldSelection
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="FieldSelection"/> class.
        /// </summary>
        /// <param name="selection">
        /// The query field selection.
        /// </param>
        /// <param name="field">
        /// The <see cref="ObjectField"/> the <paramref name="selection"/>
        /// referrs to.
        /// </param>
        /// <param name="responseName">
        /// The name the field shall have in the query result.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="field"/> is <c>null</c>
        /// or
        /// <paramref name="selection"/> is <c>null</c>
        /// or
        /// <paramref name="responseName"/> is <c>null</c>
        /// or
        /// <paramref name="responseName"/> is <see cref="string.Empty"/>.
        /// </exception>
        public FieldSelection(FieldNode selection, ObjectField field, string responseName)
        {
            if (string.IsNullOrEmpty(responseName))
            {
                throw new ArgumentNullException(nameof(responseName));
            }

            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
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
