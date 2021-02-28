using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A property that belongs to a Node or Relationship.
    /// </summary>
    public class Property : Expression
    {
        public override ClauseKind Kind => ClauseKind.Property;
        private readonly Expression _container;
        private readonly PropertyLookup _name;

        public Property(Expression container, PropertyLookup name)
        {
            _container = container;
            _name = name;
        }

        public static Property Create(INamed parentContainer, string name)
        {
            SymbolicName? requiredSymbolicName;
            try
            {
                requiredSymbolicName = parentContainer.GetRequiredSymbolicName();
            }
            catch (Exception e)
            {
                throw new ArgumentException("A property derived from a node or a relationship needs a parent with a symbolic name.");
            }
            return new Property(requiredSymbolicName, new PropertyLookup(name));
        }

        public static Property Create(Expression container, string name) => new (container, new PropertyLookup(name));

        //public Operation To(Expression expression) => Operations.Set(this, expression);

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            _container.Visit(cypherVisitor);
            _name.Visit(cypherVisitor);
            cypherVisitor.Leave(this);
        }
    }
}
