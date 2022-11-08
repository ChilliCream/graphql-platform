namespace HotChocolate.Stitching.Requests;

public interface IStitchingContext
{
    IRemoteRequestExecutor GetRemoteRequestExecutor(string schemaName);

    ISchema GetRemoteSchema(string schemaName);
}
