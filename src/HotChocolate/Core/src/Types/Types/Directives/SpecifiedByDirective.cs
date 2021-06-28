#nullable enable

namespace HotChocolate.Types
{
    public sealed class SpecifiedByDirective
    {
        public SpecifiedByDirective(string url)
        {
            Url = url;
        }

        public string Url { get; }
    }
}
