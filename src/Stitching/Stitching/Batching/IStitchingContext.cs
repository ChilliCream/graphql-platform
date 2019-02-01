using System;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IStitchingContext
    {
        IRemoteQueryClient GetRemoteQueryClient(string schemaName);
    }
}
