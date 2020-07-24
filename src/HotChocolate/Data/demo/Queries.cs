using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Demo
{
    [UseFilter(typeof(MongoFilterConvention))]
    [ExtendObjectType(Name = "Query")]
    public class Queries
    {

    }
}
