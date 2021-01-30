namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Exposes methods to add properties with values to nodes or relationships.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExposesProperties<out T> where T : PatternElement, IPropertyContainer
    {
        T WithProperties(MapExpression newProps);
        T WithProperties(params object[] keysAndValues);
    }
}
