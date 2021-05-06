namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Exposes methods to add properties with values to nodes or relationships.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExposesProperties<out T> where T :  IPropertyContainer
    {
        T WithProperties(MapExpression newProperties);
        T WithProperties(params object[] keysAndValues);
    }
}
