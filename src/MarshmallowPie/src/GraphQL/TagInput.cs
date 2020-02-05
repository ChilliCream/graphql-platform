namespace MarshmallowPie.GraphQL
{
    public class TagInput
    {
        public TagInput(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }
    }
}
