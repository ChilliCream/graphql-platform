namespace HotChocolate.Stitching.Requests
{
    public interface IStitchingContext
    {
        IRemoteRequestExecutor GetRemoteRequestExecutor(NameString schemaName);

        ISchema GetRemoteSchema(NameString schemaName);
    }
}
