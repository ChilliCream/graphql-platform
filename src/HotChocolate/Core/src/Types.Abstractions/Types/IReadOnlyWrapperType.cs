namespace HotChocolate.Types;

public interface IReadOnlyWrapperType : IReadOnlyTypeDefinition
{
    IReadOnlyTypeDefinition Type { get; }
}
