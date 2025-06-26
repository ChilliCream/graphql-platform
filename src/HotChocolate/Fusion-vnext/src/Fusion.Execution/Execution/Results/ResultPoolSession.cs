namespace HotChocolate.Fusion.Execution;

public class ResultPoolSession : IDisposable
{
    public ObjectResult RentObjectResult()
    {
        return new ObjectResult();
    }

    public LeafFieldResult RentLeafFieldResult()
    {
        return new LeafFieldResult();
    }

    public ListFieldResult RentListFieldResult()
    {
        return new ListFieldResult();
    }

    public ObjectFieldResult RentObjectFieldResult()
    {
        return new ObjectFieldResult();
    }

    public ObjectListResult RentObjectListResult()
    {
        return new ObjectListResult();
    }

    public NestedListResult RentNestedListResult()
    {
        return new NestedListResult();
    }

    public LeafListResult RentLeafListResult()
    {
        return new LeafListResult();
    }

    public void Dispose()
    {
    }
}
