namespace Espresso.Abstractions;

public interface IScalarValueParserFactory
{
    IScalarValueParser<TIn, TOut> Create<TIn, TOut>(string typeName);
}
