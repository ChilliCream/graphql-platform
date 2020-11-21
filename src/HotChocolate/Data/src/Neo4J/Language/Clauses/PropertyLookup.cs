namespace HotChocolate.Data.Neo4J.Language
{
    public class PropertyLookup : Expression
    {
        private readonly string _propertyKeyName;

        public PropertyLookup(string propertyKeyName)
        {
            _propertyKeyName = propertyKeyName;
        }

        public string GetPropertyKeyName() => _propertyKeyName;
    }
}