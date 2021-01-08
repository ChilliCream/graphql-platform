using System.Collections.Generic;
using System.Net;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Contains the information that are needed to generate a resultBuilder
    /// </summary>
    public class ResultBuilderDescriptor: ICodeDescriptor
    {
        /// <summary>
        /// The return type of the result builder. This should be the same descriptor that is used to generate:
        ///  - EntityType
        ///  - ResultType
        ///  - ResultInfo
        /// </summary>
        public TypeDescriptor ResultType { get; set; }

        /// <summary>
        /// The name of the root type that is received by the transport layer.
        /// e.g. JsonElement
        /// </summary>
        public string TransportResultRootTypeName { get; set; }

        /// <summary>
        /// A set of all type tuples, that represent the required ILeafValueParser<serializedType, runtimeType> of this ResultBuilderDescriptor.
        /// </summary>
        public IReadOnlyCollection<(string serializedType, string runtimeType)> ValueParsers { get; set; }
    }
}
