namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Context while rendering a statement.
    /// </summary>
    public interface IStatementContext<T>
    {
        string GetParameterName(Parameter<T> parameter);
        bool IsRenderConstantsAsParameters();
    }
}
