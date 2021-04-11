using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class PropertyContainer : IPropertyContainer
    {
        public SymbolicName? GetSymbolicName()
        {
            throw new System.NotImplementedException();
        }

        public SymbolicName GetRequiredSymbolicName()
        {
            throw new System.NotImplementedException();
        }

        public Property Property(string name)
        {
            return Property(new [] {name});
        }

        public Property Property(params string[] names)
        {
            return Language.Property.Create(this, names);
        }

        public Property Property(Expression lookup)
        {
            return Language.Property.Create(this, lookup);
        }

        public MapProjection Project(List<object> entries)
        {
            return Project(entries.ToArray());
        }

        public MapProjection Project(params object[] entries)
        {
            return GetRequiredSymbolicName().Project(entries);
        }
    }
}
