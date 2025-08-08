namespace HotChocolate.Execution.DependencyInjection;

internal interface IFactory<out T>
{
    T Create();
}

internal interface IFactory<out TOut, in TIn>
{
    TOut Create(TIn input);
}
