namespace StrawberryShake.Configuration
{
    public interface IClientOptions
    {
        IValueSerializerCollection GetValueSerializers(string clientName);

        IResultParserCollection GetResultParsers(string clientName);

        IOperationFormatter GetOperationFormatter(string clientName);

        OperationDelegate<T> GetOperationPipeline<T>(string clientName)
            where T : IOperationContext;
    }
}
