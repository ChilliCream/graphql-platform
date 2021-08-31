namespace HotChocolate
{
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
    }
}
