using System;

namespace HotChocolate.Stitching
{
    public interface IStitchingContext
        : IObservable<IRemoteRequestExecutor>
    {
        IRemoteRequestExecutor GetRemoteQueryClient(NameString schemaName);

        ISchema GetRemoteSchema(NameString schemaName);
    }
}
