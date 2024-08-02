using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class EntityIdFactoryDescriptorMapper
{
    public static void Map(ClientModel model, IMapperContext context)
    {
        var entities = new List<EntityIdDescriptor>();

        foreach (var entity in model.Entities)
        {
            var fields = new List<ScalarEntityIdDescriptor>();

            foreach (var field in entity.Fields)
            {
                if (field.Type.NamedType() is ILeafType leafType)
                {
                    fields.Add(
                        new ScalarEntityIdDescriptor(
                            field.Name,
                            leafType.Name,
                            model.Schema.GetOrCreateTypeInfo(leafType.GetSerializationType())));
                }
            }

            entities.Add(
                new EntityIdDescriptor(entity.Name, entity.Name, fields));
        }

        context.Register(
            new EntityIdFactoryDescriptor(
                context.ClientName + "EntityIdFactory",
                entities,
                NamingConventions.CreateStateNamespace(context.Namespace)));
    }
}
