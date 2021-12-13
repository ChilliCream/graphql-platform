#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents an input field. Input fields can be arguments of fields
/// or fields of an input objects.
/// </summary>
public interface IInputField : IField, IInputFieldInfo
{
}
