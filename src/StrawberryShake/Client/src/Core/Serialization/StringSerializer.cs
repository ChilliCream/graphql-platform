namespace StrawberryShake.Serialization
{
    public class StringSerializer
        : ILeafValueParser<string, string>
        , IInputValueFormatter
    {
        public string TypeName => BuiltInTypeNames.String;

        public string Parse(string serializedValue) => serializedValue;

        public object Format(object runtimeValue) => runtimeValue;
    }
}
