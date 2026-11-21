using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionOverfetching.Card;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("card")
            .Type<ProductCardType>()
            .Resolve(_ => CardData.Card);

        descriptor
            .Field("cardC")
            .Type<ProductCardType>()
            .Resolve(_ => CardData.CardC);
    }
}
