using System.Collections.Generic;
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
            foreach (var descriptor in CollectEnumDescriptors(model, context))
            {
                context.Register(descriptor.Name, descriptor);
            }
        }

        public static IEnumerable<EnumDescriptor> CollectEnumDescriptors(
            ClientModel model,
            IMapperContext context)
        {
            return model.LeafTypes
                .OfType<EnumTypeModel>()
                .Select(
                    enumType =>
                        new EnumDescriptor(
                            enumType.Name,
                            context.Namespace,
                            enumType.Values
                                .Select(value =>
                                    new EnumValueDescriptor(value.Name, value.Value.Name))
                                .ToList()));
        }
    }
}
