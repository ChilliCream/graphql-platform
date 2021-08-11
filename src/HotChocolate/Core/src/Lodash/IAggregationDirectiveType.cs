using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public interface IAggregationDirectiveType : ITypeSystemObject
    {
        AggregationOperation CreateOperation(DirectiveNode directive);
    }
}
