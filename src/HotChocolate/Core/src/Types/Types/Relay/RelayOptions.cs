namespace HotChocolate.Types.Relay
{
    /// <summary>
    /// Represents relay options.
    /// </summary>
    public class RelayOptions
    {
        /// <summary>
        /// If set to <c>true</c> the mutation payloads are rewritten to expose
        /// a query field that gives access to the query root type.
        /// </summary>
        public bool AddQueryFieldsToMutations { get; set; }
    }
}
