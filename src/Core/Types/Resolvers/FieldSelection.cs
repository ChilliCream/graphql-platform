using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// Represents a query field selection and provides access to the
    /// <see cref="FieldNode"/> and actual <see cref="ObjectField"/>
    /// to which the <see cref="FieldNode"/> referrs to.
    /// </summary>
    public sealed class FieldSelection
        : IEquatable<FieldSelection>
    {
        private readonly ImmutableHashSet<FieldNode> _nodes;

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
        private FieldSelection(
            FieldNode selection,
            ObjectField field,
            string responseName,
            ImmutableHashSet<FieldNode> nodes)
        {
            if (string.IsNullOrEmpty(responseName))
            {
                throw new ArgumentNullException(nameof(responseName));
            }

            Selection = selection
                ?? throw new ArgumentNullException(nameof(selection));
            Field = field
                ?? throw new ArgumentNullException(nameof(field));
            ResponseName = responseName;
            _nodes = nodes
                ?? throw new ArgumentNullException(nameof(nodes));
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

        /// <summary>
        /// Gets the syntax nodes of which this selection consists.
        /// If there are more than one node than this field was merged.
        /// </summary>
        /// <value>
        /// Returns the syntax nodes of which this selection consists.
        /// </value>
        public IReadOnlyCollection<FieldNode> Nodes => _nodes;

        /// <summary>
        /// Merge another field node into this selection.
        /// </summary>
        /// <param name="other">
        /// The other field node.
        /// </param>
        /// <returns>
        /// Returns a new field selection that combines
        /// the other field node into this selection.
        /// </returns>
        public FieldSelection Merge(FieldNode other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new FieldSelection
            (
                MergeField(Selection, other),
                Field,
                ResponseName,
                _nodes.Add(other)
            );
        }

        public bool Equals(FieldSelection other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return (other.ResponseName.EqualsOrdinal(ResponseName)
                && ReferenceEquals(other.Field, Field)
                && _nodes.SetEquals(other._nodes));
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as FieldSelection);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (ResponseName?.GetHashCode() ?? 0) * 3;
                hash = hash ^ (Field.DeclaringType.Name.GetHashCode() * 7);
                hash = hash ^ (Field.Name.GetHashCode() * 7);
                foreach (FieldNode field in _nodes)
                {
                    hash = hash ^ (field.GetHashCode() * 397);
                }
                return hash;
            }
        }

        public override string ToString()
        {
            // this string representation is only for debugging purpose.
            var sb = new StringBuilder();

            sb.Append(Field.DeclaringType.Name);
            sb.Append('.');
            sb.Append(Field.Name);
            sb.Append(':');
            sb.Append(Field.Type.Visualize());

            if (!Field.Name.Equals(ResponseName))
            {
                sb.Append(" AS ");
                sb.Append(ResponseName);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates a new field selection.
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
        /// <returns>
        /// Returns a new field selection.
        /// </returns>
        public static FieldSelection Create(
            FieldNode selection,
            ObjectField field,
            string responseName)
        {
            return new FieldSelection
            (
                selection,
                field,
                responseName,
                ImmutableHashSet<FieldNode>.Empty.Add(selection)
            );
        }

        private static FieldNode MergeField(FieldNode original, FieldNode other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (original.SelectionSet == null
                && other.SelectionSet == null
                && other.Directives.Count == 0)
            {
                return original;
            }

            var directives = new List<DirectiveNode>(original.Directives);
            directives.AddRange(other.Directives);

            SelectionSetNode selectionSet =
                (original.SelectionSet == null || other.SelectionSet == null)
                    ? original.SelectionSet ?? other.SelectionSet
                    : MergeSelectionSet(
                            original.SelectionSet,
                            other.SelectionSet);

            return new FieldNode
            (
                original.Location,
                original.Name,
                original.Alias,
                directives,
                original.Arguments,
                selectionSet
            );
        }

        private static SelectionSetNode MergeSelectionSet(
            SelectionSetNode original,
            SelectionSetNode other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other.Selections.Count == 0)
            {
                return original;
            }

            var selections = new List<ISelectionNode>(original.Selections);
            selections.AddRange(other.Selections);

            return new SelectionSetNode(original.Location, selections);
        }
    }
}
