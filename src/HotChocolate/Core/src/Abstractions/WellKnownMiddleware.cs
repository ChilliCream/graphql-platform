namespace HotChocolate;

/// <summary>
/// Provides keys that identify well-known middleware components.
/// </summary>
public static class WellKnownMiddleware
{
    /// <summary>
    /// This key identifies the paging middleware.
    /// </summary>
    public const string Paging = "HotChocolate.Types.Paging";

    /// <summary>
    /// This key identifies the projection middleware.
    /// </summary>
    public const string Projection = "HotChocolate.Data.Projection";

    /// <summary>
    /// This key identifies the filtering middleware.
    /// </summary>
    public const string Filtering = "HotChocolate.Data.Filtering";

    /// <summary>
    /// This key identifies the sorting middleware.
    /// </summary>
    public const string Sorting = "HotChocolate.Data.Sorting";

    /// <summary>
    /// This key identifies the DataLoader middleware.
    /// </summary>
    public const string DataLoader = "HotChocolate.Fetching.DataLoader";

    /// <summary>
    /// This key identifies the relay global ID middleware.
    /// </summary>
    public const string GlobalId = "HotChocolate.Types.GlobalId";

    /// <summary>
    /// This key identifies the single or default middleware.
    /// </summary>
    public const string SingleOrDefault = "HotChocolate.Data.SingleOrDefault";

    /// <summary>
    /// This key identifies the DbContext middleware.
    /// </summary>
    public const string DbContext = "HotChocolate.Data.EF.UseDbContext";

    /// <summary>
    /// This key identifies the ToList middleware.
    /// </summary>
    public const string ToList = "HotChocolate.Data.EF.ToList";

    /// <summary>
    /// The key identifies the resolver service scope middleware.
    /// </summary>
    public const string ResolverServiceScope = "HotChocolate.Resolvers.ServiceScope";

    /// <summary>
    /// This key identifies a pooled service middleware.
    /// </summary>
    public const string PooledService = "HotChocolate.Resolvers.PooledService";

    /// <summary>
    /// This key identifies a resolver service middleware.
    /// </summary>
    public const string ResolverService = "HotChocolate.Resolvers.ResolverService";

    /// <summary>
    /// This key identifies the mutation convention middleware.
    /// </summary>
    public const string MutationArguments = "HotChocolate.Types.Mutations.Arguments";

    /// <summary>
    /// This key identifies the mutation convention middleware.
    /// </summary>
    public const string MutationErrors = "HotChocolate.Types.Mutations.Errors";
}
