// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution.DependencyInjection;

public interface IFactory<out T>
{
    T Create();
}

public interface IFactory<out TOut, in TIn>
{
    TOut Create(TIn input);
}
