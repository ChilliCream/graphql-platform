
namespace SkinnyLatte.Types;

public abstract class NamedType
    : TypeSystemMember
    , IDirectiveProvider
{
    protected NamedType(
        string name, 
        IReadOnlyDictionary<string, object?> contextData,
        object directives) 
        : base(name, contextData)
    {
        Directives = directives;
    }

    public object Directives { get; }
}