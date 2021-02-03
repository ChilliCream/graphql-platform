using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class EnumDescriptorMapper
    {
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            foreach (EnumTypeModel enumType in model.LeafTypes.OfType<EnumTypeModel>())
            {
                context.Register(
                    enumType.Name,
                    new EnumDescriptor(
                        enumType.Name,
                        context.Namespace,
                        enumType.Values
                            .Select(value => new EnumValueDescriptor(value.Name, value.Value.Name))
                            .ToList()));
            }
        }
    }
}
