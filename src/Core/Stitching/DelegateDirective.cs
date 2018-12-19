using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class DelegateDirective
    {
        public string Operation { get; set; } = OperationType.Query.ToString();

        public string Path { get; set; }
    }
}
