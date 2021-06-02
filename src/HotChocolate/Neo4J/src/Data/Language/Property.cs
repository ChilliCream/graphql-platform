using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A property that belongs to a property container (either Node or Relationship).
    /// </summary>
    public class Property : Expression
    {
        private Property(
            INamed? container,
            Expression containerReference,
            List<PropertyLookup> names)
        {
            Container = container;
            ContainerReference = containerReference;
            Names = names;
        }

        public override ClauseKind Kind => ClauseKind.Property;

        public List<PropertyLookup> Names { get; }

        public INamed? Container { get; }

        public Expression ContainerReference { get; }

        private static SymbolicName ExtractRequiredSymbolicName(INamed parentContainer)
        {
            try
            {
                return parentContainer.RequiredSymbolicName;
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(Neo4JResources.Language_NeedsAParentWithSymbolicName);
            }
        }

        public Operation To(Expression expression) => Operations.Set(this, expression);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            ContainerReference.Visit(cypherVisitor);
            Names.ForEach(name => name.Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }

        public static Property Create(INamed parentContainer, params string[] names)
        {
            SymbolicName requiredSymbolicName = ExtractRequiredSymbolicName(parentContainer);
            return new Property(parentContainer,
                requiredSymbolicName,
                CreateListOfChainedNames(names));
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
                new List<PropertyLookup> { PropertyLookup.ForExpression(lookup) });
        }

        public static Property Create(Expression containerContainer, Expression lookup)
        {
            return new(
                null,
                containerContainer,
                new List<PropertyLookup> { PropertyLookup.ForExpression(lookup) });
        }

        private static List<PropertyLookup> CreateListOfChainedNames(params string[] names)
        {
            Ensure.IsNotEmpty(names, "The properties name is required.");
            return names.Length == 1
                ? new List<PropertyLookup> { PropertyLookup.ForName(names[0]) }
                : names.Select(PropertyLookup.ForName).ToList();
        }
    }
}
