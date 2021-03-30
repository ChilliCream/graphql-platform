using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A container having properties. A property container must be <see cref="INamed"/> with a non empty name to
    /// be able to refer to properties.
    /// </summary>
    public interface IPropertyContainer : INamed
    {
        public Property Property(string name);
        public Property Property(params string[] names);
        public Property Property(Expression lookup);
        public MapProjection Project(List<object> entries);
        public MapProjection Project(params object[] entries);
        //public Operation Mutate(Parameter parameter);
        //public Operation Mutate(MapExpression properties);
    }
}
