using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class EntityTypeDescriptorMapper
    {
        public static void Map(ClientModel model, IMapperContext context)
        {
            context.Register(CollectEntityTypes(model, context));
        }

        private static IEnumerable<EntityTypeDescriptor> CollectEntityTypes(
            ClientModel model,
            IMapperContext context)
        {
            var entityTypes = new Dictionary<NameString, HashSet<NameString>>();
            var descriptions = new Dictionary<NameString, string>();

            foreach (OperationModel operation in model.Operations)
            {
                foreach (var outputType in operation.OutputTypes.Where(t => !t.IsInterface))
                {
                    INamedType namedType = outputType.Type.NamedType();
                    descriptions[namedType.Name] = outputType.Description;
                    if (outputType.Type.NamedType().IsEntity())
                    {
                        if (!entityTypes.TryGetValue(
                            namedType.Name,
                            out HashSet<NameString>? components))
                        {
                            components = new HashSet<NameString>();
                            entityTypes.Add(namedType.Name, components);
                        }

                        components.Add(outputType.Name);
                    }
                }
            }

            foreach (KeyValuePair<NameString, HashSet<NameString>> entityType in entityTypes)
            {
                descriptions.TryGetValue(entityType.Key, out var description);
                yield return new EntityTypeDescriptor(
                    entityType.Key,
                    context.Namespace,
                    entityType.Value
                        .Select(name => context.Types.Single(t => t.RuntimeType.Name.Equals(name)))
                        .OfType<ComplexTypeDescriptor>()
                        .ToList(),
                    description);
            }
        }
    }
}
