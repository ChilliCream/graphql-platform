namespace StrawberryShake.CodeGeneration
{
    public class OutputFieldDescriptor
        : ICodeDescriptor
    {
        public OutputFieldDescriptor(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public string Type { get; }
    }
}
