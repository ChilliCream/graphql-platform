using System.Reflection;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public interface ISortFieldDefinition
        : IHasScope
    {
        public MemberInfo? Member { get; }

        public ISortFieldHandler? Handler { get; }
    }
}
