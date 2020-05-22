namespace HotChocolate.Execution
{
    public delegate IRequestExecutor CreateRequestExecutor(
        ISchema schema, RequestDelegate pipeline);
}