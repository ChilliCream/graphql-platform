using System.Collections.Generic;
using System.Net;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Contains the information that are needed to generate a resultBuilder
    /// </summary>
    public class ResultBuilderDescriptor : ICodeDescriptor
    {
        public ResultBuilderDescriptor(
            TypeDescriptor resultType, 
            IReadOnlyCollection<ValueParserDescriptor> valueParsers)
        {
            ResultType = resultType;
            ValueParsers = valueParsers;
        }

        /// <summary>
        /// The return type of the result builder.
        /// This should be the same descriptor that is used to generate:
        ///  - EntityType
        ///  - ResultType
        ///  - ResultInfo
        /// </summary>
        public TypeDescriptor ResultType { get; }

        /// <summary>
        /// A set of all type tuples, that represent the required
        /// <see cref="ILeafValueParser{serializedType, runtimeType}" /> of this
        /// <see cref="ResultBuilderDescriptor" />.
        /// </summary>
        public IReadOnlyCollection<ValueParserDescriptor> ValueParsers { get; }
    }
}
