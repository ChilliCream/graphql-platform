using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Contains the information that are needed to generate a resultBuilder
    /// </summary>
    public class ResultBuilderDescriptor : ICodeDescriptor
    {
        private readonly string _name;

        public ResultBuilderDescriptor(
            string name,
            NamedTypeDescriptor resultNamedType,
            IReadOnlyCollection<ValueParserDescriptor> valueParsers)
        {
            _name = name;
            ResultNamedType = resultNamedType;
            ValueParsers = valueParsers;
        }

        public NameString Name => NamingConventions.ResultBuilderNameFromTypeName(_name);

        /// <summary>
        /// The return type of the result builder.
        /// This should be the same descriptor that is used to generate:
        ///  - EntityType
        ///  - ResultType
        ///  - ResultInfo
        /// </summary>
        public NamedTypeDescriptor ResultNamedType { get; }

        /// <summary>
        /// A set of all type tuples, that represent the required
        /// <see cref="ILeafValueParser{TSerialized,TRuntime}" /> of this
        /// <see cref="ResultBuilderDescriptor" />.
        /// </summary>
        public IReadOnlyCollection<ValueParserDescriptor> ValueParsers { get; }
    }
}
