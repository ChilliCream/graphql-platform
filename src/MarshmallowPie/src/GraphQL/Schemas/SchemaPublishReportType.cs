using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class SchemaPublishReportType : ObjectType<SchemaPublishReport>
    {
        protected override void Configure(IObjectTypeDescriptor<SchemaPublishReport> descriptor)
        {
            descriptor.AsNode()
                .IdField(t => t.Id)
                .NodeResolver((ctx, id) =>
                    ctx.DataLoader<SchemaPublishReportByIdDataLoader>().LoadAsync(
                        id, ctx.RequestAborted));

            descriptor.Ignore(t => t.SchemaVersionId);
            descriptor.Ignore(t => t.EnvironmentId);
        }
    }
}
