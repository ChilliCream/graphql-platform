using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class ClientDescriptorMapper
    {
        public static void Map(ClientModel model, IMapperContext context) =>
            context.Register(
                new ClientDescriptor(
                    context.ClientName,
                    context.Namespace,
                    context.Operations.ToList()));
    }
}
