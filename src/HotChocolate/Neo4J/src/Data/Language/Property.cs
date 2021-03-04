using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A property that belongs to a property container (either Node or Relationship).
    /// </summary>
    public class Property : Expression
    {
        public override ClauseKind Kind => ClauseKind.Property;
        private readonly INamed? _container;
        private readonly Expression _containerReference;
        private readonly List<PropertyLookup> _names;

        private Property(INamed? container, Expression containerReference, List<PropertyLookup> names)
        {
            _container = container;
            _containerReference = containerReference;
            _names = names;
        }

        public static Property Create(INamed parentContainer, params string[] names)
        {
            SymbolicName requiredSymbolicName = ExtractRequiredSymbolicName(parentContainer);
            return new Property(parentContainer, requiredSymbolicName, CreateListOfChainedNames(names));
        }

        public static Property Create(Expression containerReference, params string[] names)
        {
            Ensure.IsNotNull(containerReference, "The property container is required.");
            return new Property(null, containerReference, CreateListOfChainedNames(names));
        }

        public static Property Create(INamed parentContainer, Expression lookup)
        {
            SymbolicName requiredSymbolicName = ExtractRequiredSymbolicName(parentContainer);
            return new Property(
                parentContainer,
                requiredSymbolicName,
                new SingletonList<PropertyLookup>(PropertyLookup.ForExpression(lookup)));
        }

        public static Property Create(Expression containerContainer, Expression lookup)
        {
            return new Property(
                null,
                containerContainer,
                new SingletonList<PropertyLookup>(PropertyLookup.ForExpression(lookup)));
        }

        private static List<PropertyLookup> CreateListOfChainedNames(params string[] names)
        {
            Ensure.IsNotEmpty(names, "The properties name is required.");
            return names.Length == 1
                ? new SingletonList<PropertyLookup>(PropertyLookup.ForName(names[0]))
                : names.Select(PropertyLookup.ForName).ToList();
        }

        public List<PropertyLookup> GetNames() => _names;

        public INamed? GetContainer() => _container;

        private static SymbolicName ExtractRequiredSymbolicName(INamed parentContainer) {
            try {
                return parentContainer.GetRequiredSymbolicName();
            } catch (InvalidOperationException e) {
                throw new ArgumentException(
                    "A property derived from a node or a relationship needs a parent with a symbolic name.");
            }
        }

        public Operation To(Expression expression) => Operations.Set(this, expression);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _containerReference.Visit(cypherVisitor);
            _names.ForEach(name => name.Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }
    }
}
