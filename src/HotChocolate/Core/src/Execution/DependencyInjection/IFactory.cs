namespace HotChocolate.Execution.DependencyInjection;

internal interface IFactory<out T>
{
    T Create();
}
