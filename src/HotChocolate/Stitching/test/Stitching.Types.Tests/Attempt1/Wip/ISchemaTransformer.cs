using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types;

public interface ISchemaTransformer
{
    ValueTask<ITransformationResult> Transform(IServiceDefinition serviceDefinition, SchemaTransformationOptions options);
}