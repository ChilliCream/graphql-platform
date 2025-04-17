namespace Espresso.Abstractions;

public interface IScalarValueParser<in TIn, out TOut>
{
    TOut Parse(TIn serializedValue);
}
