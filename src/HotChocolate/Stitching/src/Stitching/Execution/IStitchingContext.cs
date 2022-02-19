namespace HotChocolate.Stitching.Execution;

public interface IStitchingContext
{
    IRemoteRequestScheduler GetRemoteRequestExecutor(NameString schemaName);

    ISchema GetRemoteSchema(NameString schemaName);
}
