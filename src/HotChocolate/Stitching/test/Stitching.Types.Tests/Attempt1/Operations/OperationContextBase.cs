namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public abstract class OperationContextBase
{
    public ISchemaDatabase Database { get; }

    protected OperationContextBase(ISchemaDatabase database)
    {
        Database = database;
    }
}
