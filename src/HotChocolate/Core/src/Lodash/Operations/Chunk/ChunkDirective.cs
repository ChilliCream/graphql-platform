using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class ChunkDirective
    {
        [DefaultValue(1)]
        public int? Size { get; set; }
    }
}
