using System.Collections.Generic;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class EntityIdFactoryDescriptorMapper
    {
        public static void Map(ClientModel model, IMapperContext context)
        {
            var entities = new List<EntityIdDescriptor>();

            foreach (var entity in model.Entities)
            {
                var fields = new List<EntityIdDescriptor>();

                foreach (var field in entity.Fields)
                {
                    fields.Add(new EntityIdDescriptor(
                        field.Name,
                        ((ILeafType)field.Type.NamedType()).GetSerializationType()));
                }

                entities.Add(new EntityIdDescriptor(entity.Name, entity.Name, fields));
            }

            context.Register(
                new EntityIdFactoryDescriptor("EntityIdFactory", entities, context.StateNamespace));
        }
    }
}
