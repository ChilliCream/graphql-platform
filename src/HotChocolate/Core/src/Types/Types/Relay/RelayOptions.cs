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
        public bool AddQueryFieldToMutationPayloads { get; set; }

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

        /// <summary>
        /// A function that is used to deserialize global ids when passed
        /// as arguments or on input types (expect the special <c>node</c>
        /// field. The default implementation passes the serialized id directly
        /// to the <see cref="IIdSerializer"/>, but this can be overridden if
        /// you want to act before/after and call the serializer yourself.
        /// </summary>
        public DeserializeId DeserializeIdFunction { get; set; } =
            (idSerializer, schemaName, typeName, valueType, serializedId) =>
                idSerializer.Deserialize(serializedId);
    }

    public delegate IdValue DeserializeId(
        IIdSerializer idSerializer,
        NameString schemaName,
        NameString typeName,
        Type valueType,
        string serializedId);
}
