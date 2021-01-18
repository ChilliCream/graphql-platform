using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public interface IMapperContext
    {
        string Namespace { get; }

        void Register(NameString codeTypeName, ITypeDescriptor typeDescriptor);
    }

    public static class TypeDescriptorMapper
    {
        public static EnumDescriptor Map(
            EnumTypeModel model,
            IMapperContext context)
        {
        }

        private static void CollectTypes(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors)
        {
            foreach (OperationModel operation in model.Operations)
            {
                foreach (var outputType in operation.OutputTypes.Where(t => !t.IsInterface))
                {
                    if (!typeDescriptors.TryGetValue(
                        outputType.Name,
                        out TypeDescriptorModel descriptorModel))
                    {
                        // TODO : how do we find out if this is an entity.
                        descriptorModel = new TypeDescriptorModel(
                            outputType,
                            new TypeDescriptor(
                                outputType.Name,
                                context.Namespace,
                                outputType.Implements.Select(t => t.Name).ToList(),
                                kind: TypeKind.EntityType,
                                graphQLTypeName: outputType.Type.Name));
                        typeDescriptors.Add(outputType.Name, descriptorModel);
                    }
                }

                foreach (var outputType in operation.OutputTypes.Where(t => t.IsInterface))
                {
                    if (!typeDescriptors.TryGetValue(
                        outputType.Name,
                        out TypeDescriptorModel descriptorModel))
                    {
                        // TODO : how do we find out if this is an entity.
                        descriptorModel = new TypeDescriptorModel(
                            outputType,
                            new TypeDescriptor(
                                outputType.Name,
                                context.Namespace,
                                outputType.Implements.Select(t => t.Name).ToList(),
                                kind: TypeKind.EntityType,
                                graphQLTypeName: outputType.Type.Name,
                                isImplementedBy: ));
                        typeDescriptors.Add(outputType.Name, descriptorModel);
                    }
                }
            }
        }

        private readonly struct TypeDescriptorModel
        {
            public TypeDescriptorModel(OutputTypeModel typeModel, TypeDescriptor typeDescriptor)
            {
                TypeModel = typeModel;
                TypeDescriptor = typeDescriptor;
            }

            public OutputTypeModel TypeModel { get; }

            public TypeDescriptor TypeDescriptor { get; }
        }
    }

    public static class EnumAnalyzerToDescriptorMapper
    {
        public static EnumDescriptor Map(
            EnumTypeModel model,
            IMapperContext context)
        {
            return new(
                model.Name,
                context.Namespace,
                model.Values
                    .Select(enumValue => new EnumElementDescriptor(enumValue.Name))
                    .ToList());
        }
    }

    public static class EntityTypeDescriptorMapper
    {
        public static IEnumerable<EntityTypeDescriptor> Map(
            ClientModel model,
            IMapperContext context)
        {
            var entityModels = new Dictionary<NameString, EntityModel>();

            CollectTypes(model, entityModels);

            foreach (var entityModel in entityModels.Values)
            {
                yield return new EntityTypeDescriptor
            }
        }

        private static void CollectTypes(
            ClientModel model,
            Dictionary<NameString, EntityModel> entityModels)
        {
            foreach (OperationModel operation in model.Operations)
            {
                foreach (var outputType in operation.OutputTypes)
                {
                    if (!entityModels.TryGetValue(
                        outputType.Type.Name,
                        out EntityModel entityModel))
                    {
                        entityModel = new EntityModel(outputType.Name);
                        entityModels.Add(entityModel.Name, entityModel);
                    }

                    foreach (var field in outputType.Fields)
                    {
                        if (!entityModel.Fields.ContainsKey(field.Field.Name))
                        {
                            entityModel.Fields.Add(field.Field.Name, field.Field);
                        }
                    }
                }
            }
        }

        private class EntityModel
        {
            public EntityModel(NameString name)
            {
                Name = name;
            }

            public NameString Name { get; }

            public Dictionary<NameString, IOutputField> Fields { get; } = new();
        }
    }
}
