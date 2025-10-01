using System.IO.Pipelines;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonSerializer
{
    public void WriteTo(PipeWriter writer)
    {
        throw new NotImplementedException();
    }
}
