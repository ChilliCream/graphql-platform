using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IReadOnlyInputValueDefinition : IReadOnlyFieldDefinition
{
    IValueNode? DefaultValue { get; }
}
