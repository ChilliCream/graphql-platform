#nullable enable

namespace HotChocolate.Execution
{
    public interface IResultData
    {
        IResultData? Parent { get; }
    }
}
