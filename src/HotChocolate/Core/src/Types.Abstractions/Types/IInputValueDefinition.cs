using HotChocolate.Language;

namespace HotChocolate.Types;

public interface IInputValueDefinition : IFieldDefinition
{
    IValueNode? DefaultValue { get; }
}
