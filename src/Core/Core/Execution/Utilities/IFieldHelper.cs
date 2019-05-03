using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    public interface IFieldHelper
        : IFieldCollector
    {
        FieldDelegate CreateMiddleware(FieldSelection fieldSelection);
    }
}
