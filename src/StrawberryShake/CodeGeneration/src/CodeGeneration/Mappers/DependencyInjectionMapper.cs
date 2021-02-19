using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class DependencyInjectionMapper
    {
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            context.Register(
                new DependencyInjectionDescriptor(
                    context.ClientName,
                    context.Namespace,
                    EntityTypeDescriptorMapper.CollectEntityTypes(model, context).ToList(),
                    context.Operations.ToList(),
                    TypeDescriptorMapper
                        .CollectTypeDescriptors(model, context)
                        .Select(x => x.Item2)
                        .ToList(),
                    EnumDescriptorMapper.CollectEnumDescriptors(model, context).ToList()));
        }
    }
}
