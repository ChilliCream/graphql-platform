using System.Collections.Generic;
using System.Linq;
using HotChocolate;
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
                    modelOperation.ResultType.Name,
                    new ResultBuilderDescriptor(
                        context.Types.Single(t => t.Name.Equals(modelOperation.ResultType.Name)),
                        modelOperation.LeafTypes.Select(
                            leafType => new ValueParserDescriptor(
                                leafType.SerializationType,
                                leafType.RuntimeType,
                                leafType.Name)).ToList()));
            }
        }
    }

    public class EntityIdFactoryDescriptorMapper
    {
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            var map = new Dictionary<NameString, EntityIdDescriptor>();

            foreach (OperationModel modelOperation in model.Operations)
            {
                
            }
        }
    }
}
