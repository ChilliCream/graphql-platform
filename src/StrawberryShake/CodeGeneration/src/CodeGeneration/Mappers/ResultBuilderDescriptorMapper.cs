using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.NamingConventions;

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
                var resultTypeName = ResultRootTypeNameFromTypeName(modelOperation.ResultType.Name);
                context.Register(
                    modelOperation.Name,
                    new ResultBuilderDescriptor(
                        modelOperation.Name,
                        context.Types.Single(t => t.Name.Equals(resultTypeName)),
                        modelOperation.LeafTypes.Select(
                            leafType => new ValueParserDescriptor(
                                leafType.SerializationType,
                                leafType.RuntimeType,
                                leafType.Name)).ToList()));
            }
        }
    }
}
