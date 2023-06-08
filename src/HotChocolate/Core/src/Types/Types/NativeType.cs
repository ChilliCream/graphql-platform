namespace HotChocolate.Types;

/// <summary>
/// Can be used to make the system imply the Graph QL object type
/// from the underlying regular .NET type.
/// </summary>
public class NativeType<T> : FluentWrapperType
{
}
