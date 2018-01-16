using System.Collections.Immutable;

namespace Zeus
{
    public interface IResolverContext
    {
        IImmutableStack<object> Path { get; }

        T Parent<T>();

        T Argument<T>(string name);
    }
}
