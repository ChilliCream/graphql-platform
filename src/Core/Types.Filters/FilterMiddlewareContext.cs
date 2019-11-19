namespace HotChocolate.Types.Filters
{
    public class FilterMiddlewareContext
    {
        public string ArgumentName { get; }
        public FilterMiddlewareContext(string argumentName)
        {
            ArgumentName = argumentName;
        }
        public static FilterMiddlewareContext Create(string argumentName)
        {
            return new FilterMiddlewareContext(argumentName);
        }
    }
}
