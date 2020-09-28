using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class FieldDefinitionNode
        : NamedSyntaxNode
    {
        public FieldDefinitionNode(
            Location? location,
            NameNode name,
            StringValueNode? description,
            IReadOnlyList<InputValueDefinitionNode> arguments,
            ITypeNode type,
            IReadOnlyList<DirectiveNode> directives)
            : base(location, name, directives)
        {
            Description = description;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
        }

        public override SyntaxKind Kind { get; } = SyntaxKind.FieldDefinition;

        public StringValueNode? Description { get; }

        public IReadOnlyList<InputValueDefinitionNode> Arguments { get; }

        public ITypeNode Type { get; }

        public override IEnumerable<ISyntaxNode> GetNodes()
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

            yield return Type;

            foreach (DirectiveNode directive in Directives)
            {
                yield return directive;
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
        public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public FieldDefinitionNode WithLocation(Location? location)
        {
            return new FieldDefinitionNode(
                location, Name, Description,
                Arguments, Type, Directives);
        }

        public FieldDefinitionNode WithName(NameNode name)
        {
            return new FieldDefinitionNode(
                Location, name, Description,
                Arguments, Type, Directives);
        }

        public FieldDefinitionNode WithDescription(
            StringValueNode? description)
        {
            return new FieldDefinitionNode(
                Location, Name, description,
                Arguments, Type, Directives);
        }

        public FieldDefinitionNode WithArguments(
            IReadOnlyList<InputValueDefinitionNode> arguments)
        {
            return new FieldDefinitionNode(
                Location, Name, Description,
                arguments, Type, Directives);
        }

        public FieldDefinitionNode WithType(ITypeNode type)
        {
            return new FieldDefinitionNode(
                Location, Name, Description,
                Arguments, type, Directives);
        }

        public FieldDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new FieldDefinitionNode(
                Location, Name, Description,
                Arguments, Type, directives);
        }
    }
}
