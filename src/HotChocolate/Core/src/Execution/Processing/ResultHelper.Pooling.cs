namespace HotChocolate.Execution.Processing
{
    internal sealed partial class ResultHelper
    {
        public void Clear()
        {
            _errors.Clear();
            _fieldErrors.Clear();
            _nonNullViolations.Clear();
            _extensions.Clear();
            _contextData.Clear();
            _resultOwner = new ResultMemoryOwner(_resultPool);
            _data = null;
            _path = null;
            _label = null;
            _hasNext = null;
        }
    }
}
