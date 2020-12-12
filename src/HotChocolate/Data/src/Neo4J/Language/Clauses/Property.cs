using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A property that belongs to a Node or Relationship.
    /// </summary>
    public class Property : Expression
    {
        private readonly Expression _container;
        private readonly PropertyLookup _name;

        public Property(Expression container, PropertyLookup name)
        {
            _container = container;
            _name = name;
        }

        public static Property Create(Named parentContainer, string name)
        {
            SymbolicName requiredSymbolicName;
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

        public static Property Create(Expression container, string name) => new Property(container, new PropertyLookup(name));

        //public Operation To(Expression expression) => Operations.Set(this, expression);

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _container.Visit(visitor);
            _name.Visit(visitor);
            visitor.Leave(this);
        }
    }
}
