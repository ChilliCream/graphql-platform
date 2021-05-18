using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// A container having properties. A property container must be <see cref="INamed"/> with a
    /// non empty name to be able to refer to properties.
    /// </summary>
    public interface IPropertyContainer : INamed
    {
        Property Property(string name);

        Property Property(params string[] names);

        Property Property(Expression lookup);

        MapProjection Project(List<object> entries);

        MapProjection Project(params object[] entries);
    }
}
