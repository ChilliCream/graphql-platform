using System.Reflection;

namespace HotChocolate.Types
{
    public interface IHasMemberInfo
    {
        MemberInfo Member { get; }
    }
}
