using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DelegateDirectiveType
        : DirectiveType<DelegateDirective>
    {

    }

    public class DelegateDirective
    {
        public string Path { get; set; }
    }
}
