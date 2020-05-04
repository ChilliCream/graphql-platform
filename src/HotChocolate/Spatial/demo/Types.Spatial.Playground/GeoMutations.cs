using HotChocolate.Types;

namespace Types.Spatial.Playground
{
    [ExtendObjectType(Name = "Mutation")]
    public class GeoMutations
    {
        public bool Hello()
        {
            return true;
        }
    }
}
