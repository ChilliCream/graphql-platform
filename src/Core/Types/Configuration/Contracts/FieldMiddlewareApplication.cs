namespace HotChocolate.Configuration
{
    /// <summary>
    /// This enum specified on which fields custom field
    /// middleware is applied to.
    /// </summary>
    public enum FieldMiddlewareApplication : byte
    {
        /// <summary>
        /// Custom field middleware is only applied to
        /// user-defined fields and not to introspection fields.
        /// </summary>
        UserDefinedFields = 0,

        /// <summary>
        /// Custom field middleware is applied to all fields
        /// (user-defined fields and introspection fields).
        /// </summary>
        AllFields = 1
    }
}
