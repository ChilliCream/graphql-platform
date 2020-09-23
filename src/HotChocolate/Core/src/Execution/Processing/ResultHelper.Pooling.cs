namespace HotChocolate.Execution.Processing
{
    internal sealed partial class ResultHelper
    {
        public void Reset()
        {
            _errors.Clear();
            _fieldErrors.Clear();
            _nonNullViolations.Clear();
            _resultOwner = new ResultMemoryOwner(_resultPool);
            _data = null;
        }
    }
}
