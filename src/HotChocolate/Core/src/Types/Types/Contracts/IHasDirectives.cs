#nullable enable

namespace HotChocolate.Types
{
    public interface IHasDirectives
    {
        public IDirectiveCollection Directives { get; }
    }
}
