namespace HotChocolate.Types.Descriptors;

public sealed class SchemaCompletedEventArgs : EventArgs
{
    public SchemaCompletedEventArgs(ISchema schema)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public ISchema Schema { get; }
}
