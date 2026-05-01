using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Magazines;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("magazines")
            .Type<ListType<MagazineType>>()
            .Resolve(_ => MagazineData.Magazines.Select(
                m => new Magazine { Id = m.Id, Title = m.Title }).ToList());
    }
}
