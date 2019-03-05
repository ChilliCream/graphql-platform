using System;

namespace HotChocolate.Stitching
{
    public interface IStitchingContext
        : IObservable<IRemoteQueryClient>
    {
        IRemoteQueryClient GetRemoteQueryClient(NameString schemaName);

        ISchema GetRemoteSchema(NameString schemaName);
    }
}
