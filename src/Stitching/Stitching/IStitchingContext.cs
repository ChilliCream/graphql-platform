using System;
using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public interface IStitchingContext
        : IObservable<IRemoteQueryClient>
    {
        IRemoteQueryClient GetRemoteQueryClient(NameString schemaName);
    }
}
