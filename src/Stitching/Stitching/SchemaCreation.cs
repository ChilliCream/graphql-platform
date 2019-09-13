namespace HotChocolate.Stitching
{
    /// <summary>
    /// Defines when the stitched schema shall be created.
    /// </summary>
    public enum SchemaCreation
    {
        /// <summary>
        /// Create stitched schema on first incoming request.
        /// </summary>
        OnFirstRequest,

        /// <summary>
        /// Create stitched schema on startup of the server.
        /// </summary>
        OnStartup
    }
}
