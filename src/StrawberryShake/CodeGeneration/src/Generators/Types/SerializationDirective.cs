namespace StrawberryShake.Generators.Types
{
    public class SerializationDirective
    {
        public SerializationDirective(string clrType, string serializationType)
        {
            ClrType = clrType;
            SerializationType = serializationType;
        }

        public string ClrType { get; }

        public string SerializationType { get; }
    }
}
