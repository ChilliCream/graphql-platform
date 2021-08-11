using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class ChunkDirective
    {
        [DefaultValue(0)]
        public int? Size { get; set; }
    }
}
