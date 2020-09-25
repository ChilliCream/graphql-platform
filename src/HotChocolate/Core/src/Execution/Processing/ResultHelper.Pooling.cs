namespace HotChocolate.Execution.Processing
{
    internal sealed partial class ResultHelper
    {
        public void Clear()
        {
            _errors.Clear();
            _fieldErrors.Clear();
            _nonNullViolations.Clear();
            _resultOwner = new ResultMemoryOwner(_resultPool);
            _data = null;
            _extensions = null;
        }
    }
}
