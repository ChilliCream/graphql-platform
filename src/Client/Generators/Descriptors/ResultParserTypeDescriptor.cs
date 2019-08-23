namespace StrawberryShake.Generators
{
    public class ResultParserTypeDescriptor
        : IResultParserTypeDescriptor
    {
        public ResultParserTypeDescriptor(IClassDescriptor resultDescriptor)
        {
            ResultDescriptor = resultDescriptor;
        }

        public IClassDescriptor ResultDescriptor { get; }
    }
}
