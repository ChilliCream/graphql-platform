using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class VariableDefinitionNode
        : ISyntaxNode
        , IHasDirectives
    {
        public VariableDefinitionNode(
            Location? location,
            VariableNode variable,
            ITypeNode type,
            IValueNode? defaultValue,
            IReadOnlyList<DirectiveNode> directives)
        {
            Location = location;
            Variable = variable
                ?? throw new ArgumentNullException(nameof(variable));
            Type = type
                ?? throw new ArgumentNullException(nameof(type));
            DefaultValue = defaultValue;
            Directives = directives
                ?? throw new ArgumentNullException(nameof(directives));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.VariableDefinition;

        public Location? Location { get; }

        public VariableNode Variable { get; }

        public ITypeNode Type { get; }

        public IValueNode? DefaultValue { get; }

        public IReadOnlyList<DirectiveNode> Directives { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Variable;
            yield return Type;

            if (DefaultValue is { })
            {
                yield return DefaultValue;
            }

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
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public VariableDefinitionNode WithLocation(Location? location)
        {
            return new VariableDefinitionNode(
                location, Variable, Type,
                DefaultValue, Directives);
        }

        public VariableDefinitionNode WithVariable(VariableNode variable)
        {
            return new VariableDefinitionNode(
                Location, variable, Type,
                DefaultValue, Directives);
        }

        public VariableDefinitionNode WithType(ITypeNode type)
        {
            return new VariableDefinitionNode(
                Location, Variable, type,
                DefaultValue, Directives);
        }

        public VariableDefinitionNode WithDefaultValue(IValueNode? defaultValue)
        {
            return new VariableDefinitionNode(
                Location, Variable, Type,
                defaultValue, Directives);
        }

        public VariableDefinitionNode WithDirectives(
            IReadOnlyList<DirectiveNode> directives)
        {
            return new VariableDefinitionNode(
                Location, Variable, Type,
                DefaultValue, directives);
        }
    }
}
