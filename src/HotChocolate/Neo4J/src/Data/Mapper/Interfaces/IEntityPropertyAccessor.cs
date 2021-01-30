namespace HotChocolate.Data.Neo4J
{
    internal interface IEntityPropertyAccessor
    {
        void SetNodeId(object instance, long id);
        long? GetNodeId(object instance);
    }
}
