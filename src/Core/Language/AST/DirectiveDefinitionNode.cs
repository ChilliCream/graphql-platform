using System;
using System.Collections.Generic;

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

        public NodeKind Kind { get; } = NodeKind.DirectiveDefinition;

        public Location? Location { get; }

        public NameNode Name { get; }

        public StringValueNode? Description { get; }

        public bool IsRepeatable { get; }

        public bool IsUnique => !IsRepeatable;

        public IReadOnlyList<InputValueDefinitionNode> Arguments { get; }

        public IReadOnlyList<NameNode> Locations { get; }

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
