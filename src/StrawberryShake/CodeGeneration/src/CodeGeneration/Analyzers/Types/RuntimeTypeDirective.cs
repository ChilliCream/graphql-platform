namespace StrawberryShake.CodeGeneration.Analyzers.Types
{
    public class RuntimeTypeDirective
    {
        public RuntimeTypeDirective(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
