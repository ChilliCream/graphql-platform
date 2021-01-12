using System;

namespace HotChocolate.Types.Relay
{
    /// <summary>
    /// Represents relay options.
    /// </summary>
    public class RelayOptions
    {
        /// <summary>
        /// If set to <c>true</c> the mutation payloads are rewritten to provide access to
        /// the query root type to allow better capabilities refetch data.
        /// </summary>
        public bool AddQueryFieldsToMutations { get; set; }

        /// <summary>
        /// The name of the query field on a mutation payload (default: query).
        /// </summary>
        public NameString? QueryFieldName { get; set; }

        /// <summary>
        /// A predicate that defines if the query field shall be added to
        /// the specified payload type.
        /// </summary>
        public Func<INamedType, bool> MutationPayloadPredicate { get; set; } =
            type => type.Name.Value.EndsWith("Payload", StringComparison.Ordinal);
    }
}
