using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class DirectiveDefinitionNode
        : ITypeSystemDefinitionNode
    {
        public DirectiveDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            bool isRepeatable,
            IReadOnlyList<InputValueDefinitionNode> arguments,
            IReadOnlyList<NameNode> locations)
        {
            Location = location;
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            IsRepeatable = isRepeatable;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
            Locations = locations
                ?? throw new ArgumentNullException(nameof(locations));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.DirectiveDefinition;

        public Location? Location { get; }

        public NameNode Name { get; }

        public StringValueNode? Description { get; }

        public bool IsRepeatable { get; }

        public bool IsUnique => !IsRepeatable;

        public IReadOnlyList<InputValueDefinitionNode> Arguments { get; }

        public IReadOnlyList<NameNode> Locations { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            if (Description is { })
            {
                yield return Description;
            }

            yield return Name;

            foreach (InputValueDefinitionNode argument in Arguments)
            {
                yield return argument;
            }

            foreach (NameNode location in Locations)
            {
                yield return location;
            }
        }

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public DirectiveDefinitionNode WithLocation(Location? location)
        {
            return new DirectiveDefinitionNode
            (
                location,
                Name,
                Description,
                IsRepeatable,
                Arguments,
                Locations
            );
        }

        public DirectiveDefinitionNode WithName(
            NameNode name)
        {
            return new DirectiveDefinitionNode
            (
                Location,
                name,
                Description,
                IsRepeatable,
                Arguments,
                Locations
            );
        }

        public DirectiveDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new DirectiveDefinitionNode
            (
                Location,
                Name,
                description,
                IsRepeatable,
                Arguments,
                Locations
            );
        }

        public DirectiveDefinitionNode AsRepeatable()
        {
            return new DirectiveDefinitionNode
            (
                Location,
                Name,
                Description,
                true,
                Arguments,
                Locations
            );
        }

        public DirectiveDefinitionNode AsUnique()
        {
            return new DirectiveDefinitionNode
            (
                Location,
                Name,
                Description,
                false,
                Arguments,
                Locations
            );
        }

        public DirectiveDefinitionNode WithArguments(
            IReadOnlyList<InputValueDefinitionNode> arguments)
        {
            return new DirectiveDefinitionNode
            (
                Location,
                Name,
                Description,
                IsRepeatable,
                arguments,
                Locations
            );
        }

        public DirectiveDefinitionNode WithLocations(
            IReadOnlyList<NameNode> locations)
        {
            return new DirectiveDefinitionNode
            (
                Location,
                Name,
                Description,
                IsRepeatable,
                Arguments,
                locations
            );
        }
    }
}
