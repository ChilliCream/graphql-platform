namespace Espresso.Abstractions;

public interface IInputValueFormatterFactory
{
    IInputValueFormatter<TIn, TOut> Create<TIn, TOut>(string typeName);
}
