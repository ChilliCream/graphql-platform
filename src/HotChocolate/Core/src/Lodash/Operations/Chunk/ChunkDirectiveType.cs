using HotChocolate.Types;

namespace HotChocolate.Lodash
{
    public class ChunkDirectiveType : AggregationDirectiveType<ChunkDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<ChunkDirective> descriptor)
        {
            descriptor.Name("chunk");
            descriptor.Location(DefaultDirectiveLocation);
            descriptor.Repeatable();
            descriptor.Argument(x => x.Size);
        }

        protected override AggregationOperation CreateOperation(ChunkDirective directive)
        {
            return new ChunkOperation(directive.Size!.Value);
        }
    }
}
