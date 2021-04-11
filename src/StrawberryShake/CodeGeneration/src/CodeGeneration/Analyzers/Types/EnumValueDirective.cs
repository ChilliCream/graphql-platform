namespace StrawberryShake.CodeGeneration.Analyzers.Types
{
    public class EnumValueDirective
    {
        public EnumValueDirective(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
