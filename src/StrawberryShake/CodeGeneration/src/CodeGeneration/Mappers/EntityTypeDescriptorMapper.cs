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
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            foreach (var entityTypeDescriptor in CollectEntityTypes(model, context))
            {
                context.Register(entityTypeDescriptor.GraphQLTypeName, entityTypeDescriptor);
            }
        }

        public static IEnumerable<EntityTypeDescriptor> CollectEntityTypes(
            ClientModel model,
            IMapperContext context)
        {
            {
                var entityTypes = new Dictionary<NameString, HashSet<NameString>>();

                foreach (OperationModel operation in model.Operations)
                {
                    foreach (var outputType in operation.OutputTypes.Where(t => !t.IsInterface))
                    {
                        INamedType namedType = outputType.Type.NamedType();

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
                    yield return new EntityTypeDescriptor(
                        entityType.Key,
                        context.Namespace,
                        entityType.Value
                            .Select(name => context.Types.Single(t => t.Name.Equals(name)))
                            .ToList());
                }
            }
        }
    }
}
