using System;

namespace StrawberryShake.Generators.Descriptors
{
    public class ResultParserTypeDescriptor
        : IResultParserTypeDescriptor
    {
        public ResultParserTypeDescriptor(IClassDescriptor resultDescriptor)
        {
            ResultDescriptor = resultDescriptor
                ?? throw new ArgumentNullException(nameof(resultDescriptor));
        }

        public IClassDescriptor ResultDescriptor { get; }
    }
}
