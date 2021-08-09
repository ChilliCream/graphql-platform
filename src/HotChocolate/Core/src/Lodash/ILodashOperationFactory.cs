using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public interface ILodashOperationFactory
    {
        bool TryCreateOperation(
            DirectiveNode directiveNode,
            [NotNullWhen(true)] out LodashOperation? operation);
    }
}
