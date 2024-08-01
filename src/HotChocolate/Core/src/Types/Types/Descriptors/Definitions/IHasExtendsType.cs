#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public interface IHasExtendsType
{
    /// <summary>
    /// If this is a type definition extension this is the type we want to extend.
    /// </summary>
    Type? ExtendsType { get; }
}
