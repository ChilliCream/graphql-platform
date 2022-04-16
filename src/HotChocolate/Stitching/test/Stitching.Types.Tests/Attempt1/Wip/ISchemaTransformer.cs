using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface ISchemaTransformer
{
    ValueTask<ITransformationResult> Transform(IServiceDefinition serviceDefinition, SchemaTransformationOptions options);
}