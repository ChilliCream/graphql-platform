using System;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class QueryResponse<T>
{
    public Guid? DecisionId { get; set; }
    public T? Result { get; set; }
}
