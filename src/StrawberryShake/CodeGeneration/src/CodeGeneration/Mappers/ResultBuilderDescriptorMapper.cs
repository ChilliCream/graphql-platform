using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class ResultBuilderDescriptorMapper
    {
        public static void Map(ClientModel model, IMapperContext context)
        {
            foreach (OperationModel modelOperation in model.Operations)
            {
                RuntimeTypeInfo resultType = context.GetRuntimeType(
                    modelOperation.ResultType.Name,
                    Descriptors.TypeDescriptors.TypeKind.Result);

                context.Register(
                    modelOperation.Name,
                    new ResultBuilderDescriptor(
                        new RuntimeTypeInfo(
                            CreateResultBuilderName(modelOperation.Name),
                            CreateStateNamespace(context.Namespace)),
                        context.Types.Single(t => t.RuntimeType.Equals(resultType)),
                        modelOperation.LeafTypes.Select(
                            leafType =>
                            {
                                string runtimeType =
                                    leafType.RuntimeType.Contains('.')
                                        ? leafType.RuntimeType
                                        : $"{context.Namespace}.{leafType.RuntimeType}";

                                string serializationType =
                                    leafType.SerializationType.Contains('.')
                                        ? leafType.SerializationType
                                        : $"{context.Namespace}.{leafType.SerializationType}";

                                return new ValueParserDescriptor(
                                    leafType.Name,
                                    TypeInfos.GetOrCreate(runtimeType),
                                    TypeInfos.GetOrCreate(serializationType));
                            }).ToList()));
            }
        }
    }
}
