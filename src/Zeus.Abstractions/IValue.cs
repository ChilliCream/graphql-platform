namespace Zeus.Abstractions
{
    public interface IValue
    {
        IType Type { get; }

        string Value { get; }
    }
}