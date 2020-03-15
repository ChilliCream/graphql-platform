namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultOptions
    {
        public SingleOrDefaultOptions(bool allowMultipleResults)
        {
            AllowMultipleResults = allowMultipleResults;
        }

        public bool AllowMultipleResults { get; }
    }
}
