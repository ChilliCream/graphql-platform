namespace HotChocolate.Types.Sorting
{
    public class SortMiddlewareContext
    {
        public string ArgumentName { get; }
        public SortMiddlewareContext(string argumentName)
        {
            ArgumentName = argumentName;
        }
        public static SortMiddlewareContext Create(string argumentName)
        {
            return new SortMiddlewareContext(argumentName);
        }
    }
}
