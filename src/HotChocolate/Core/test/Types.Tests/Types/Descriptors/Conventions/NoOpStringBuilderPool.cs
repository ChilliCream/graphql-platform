using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Types.Descriptors;

public class NoOpStringBuilderPool : ObjectPool<StringBuilder>
{
    public override StringBuilder Get() => new();

    public override void Return(StringBuilder obj)
    {
        obj.Clear();
    }
}
