using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class ResultBuilderDescriptorMapper
    {
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            foreach (OperationModel modelOperation in model.Operations)
            {
                context.Register(
                    modelOperation.Name,
                    new ResultBuilderDescriptor(
                        modelOperation.Name,
                        context.Types.Single(t => t.Name.Equals(modelOperation.ResultType.Name)),
                        modelOperation.LeafTypes.Select(
                            leafType => new ValueParserDescriptor(
                                leafType.SerializationType,
                                leafType.RuntimeType,
                                leafType.Name)).ToList()));
            }
        }
    }
}
