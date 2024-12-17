#nullable enable
namespace HotChocolate.Types;

public interface IIdInputValueFormatter : IInputValueFormatter
{
    object? FormatId(Type? namedType, object? originalValue);
}
