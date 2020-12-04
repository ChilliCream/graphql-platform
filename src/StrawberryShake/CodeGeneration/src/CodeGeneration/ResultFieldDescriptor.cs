namespace StrawberryShake.CodeGeneration
{
    public class ResultFieldDescriptor
        : ICodeDescriptor
    {
        public ResultFieldDescriptor(
            string name,
            string parserMethodName)
        {
            Name = name;
            ParserMethodName = parserMethodName;
        }

        public string Name { get; }

        public string ParserMethodName { get; }
    }
}
