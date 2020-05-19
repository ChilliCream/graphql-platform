namespace HotChocolate.Execution.Utilities
{
    internal sealed partial class ResultHelper : IResultHelper
    {
        public void Reset()
        {
            _errors.Clear();
            _fieldErrors.Clear();
            _nonNullViolations.Clear();
            _result = new Result(_resultPool);
            _data = null;
        }     
    }
}