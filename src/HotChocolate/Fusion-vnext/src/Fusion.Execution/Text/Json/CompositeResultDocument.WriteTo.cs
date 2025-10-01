using System.Buffers;
using System.IO.Pipelines;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteTo(PipeWriter writer)
    {
        throw new NotImplementedException();
    }

    public void WriteTo(IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }
}
