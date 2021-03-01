namespace StrawberryShake.CodeGeneration.Analyzers.Types
{
    public class SerializationTypeDirective
    {
        public SerializationTypeDirective(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
