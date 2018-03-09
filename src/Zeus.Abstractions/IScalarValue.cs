
namespace Zeus.Abstractions
{
    public interface IScalarValue
        : IValue
    {
        NamedType Type { get; }
    }
}