using System.Collections.Generic;
using HotChocolate;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Descriptors
{
    /// <summary>
    /// Contains the information that are needed to generate a resultBuilder
    /// </summary>
    public class ResultBuilderDescriptor : ICodeDescriptor
    {
        public ResultBuilderDescriptor(
            RuntimeTypeInfo runtimeType,
            INamedTypeDescriptor resultNamedType,
            IReadOnlyCollection<ValueParserDescriptor> valueParsers)
        {
            ResultNamedType = resultNamedType;
            RuntimeType = runtimeType;
            ValueParsers = valueParsers;
        }

        public NameString Name => RuntimeType.Name;

        public RuntimeTypeInfo RuntimeType { get; }

        /// <summary>
        /// The return type of the result builder.
        /// This should be the same descriptor that is used to generate:
        ///  - EntityType
        ///  - ResultType
        ///  - ResultInfo
        /// </summary>
        public INamedTypeDescriptor ResultNamedType { get; }

        /// <summary>
        /// A set of all type tuples, that represent the required
        /// <see cref="ILeafValueParser{TSerialized,TRuntime}" /> of this
        /// <see cref="ResultBuilderDescriptor" />.
        /// </summary>
        public IReadOnlyCollection<ValueParserDescriptor> ValueParsers { get; }
    }
}
