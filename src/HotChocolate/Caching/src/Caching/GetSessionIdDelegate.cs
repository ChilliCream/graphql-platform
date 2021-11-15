using HotChocolate.Execution;

namespace HotChocolate.Caching;

public delegate object? GetSessionIdDelegate(IRequestContext context);