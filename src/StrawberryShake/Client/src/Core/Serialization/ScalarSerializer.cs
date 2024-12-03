namespace StrawberryShake.Serialization;

public abstract class ScalarSerializer<T> : ScalarSerializer<T, T>
{
    protected ScalarSerializer(string typeName) : base(typeName)
    {
    }

    public override T Parse(T serializedValue) => serializedValue;

    protected override T Format(T runtimeValue) => runtimeValue;
}

public abstract class ScalarSerializer<TSerialized, TRuntime>
    : ILeafValueParser<TSerialized, TRuntime>
    , IInputValueFormatter
{
    protected ScalarSerializer(string typeName)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
    }

    public string TypeName { get; }

    public abstract TRuntime Parse(TSerialized serializedValue);

    public object? Format(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return null;
        }

        if (runtimeValue is TRuntime r)
        {
            return Format(r);
        }

        throw ThrowHelper.InputFormatter_InvalidType(
            typeof(TRuntime).FullName ?? typeof(TRuntime).Name,
            TypeName);
    }

    protected abstract TSerialized Format(TRuntime runtimeValue);
}
