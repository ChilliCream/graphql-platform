namespace HotChocolate.AspNetCore.Authorization;

public sealed class QueryResponse
{
    public Guid? DecisionId { get; set; }
    public bool Result { get; set; }
}
