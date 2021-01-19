using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Analyzers.WellKnownContextData;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public interface IMapperContext
    {
        string Namespace { get; }

        IReadOnlyCollection<NamedTypeDescriptor> Types { get; }

        void Register(NameString codeTypeName, NamedTypeDescriptor typeDescriptor);
    }

    public class MapperContext : IMapperContext
    {
        private readonly Dictionary<NameString, NamedTypeDescriptor> _types = new();

        public MapperContext(string ns)
        {
            Namespace = ns;
        }

        public string Namespace { get; }

        public IReadOnlyCollection<NamedTypeDescriptor> Types => _types.Values;

        public void Register(NameString codeTypeName, NamedTypeDescriptor typeDescriptor)
        {
            _types.Add(
                codeTypeName.EnsureNotEmpty(nameof(codeTypeName)),
                typeDescriptor ?? throw new ArgumentNullException(nameof(typeDescriptor)));
        }
    }

    public static class TypeDescriptorMapper
    {
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            var typeDescriptors = new Dictionary<NameString, TypeDescriptorModel>();
            var scalarTypeDescriptors = new Dictionary<NameString, NamedTypeDescriptor>();

            CollectTypes(model, context, typeDescriptors);
            AddProperties(model, context, typeDescriptors, scalarTypeDescriptors);

            foreach (TypeDescriptorModel descriptorModel in typeDescriptors.Values)
            {
                context.Register(
                    descriptorModel.NamedTypeDescriptor.Name,
                    descriptorModel.NamedTypeDescriptor);
            }

            foreach (NamedTypeDescriptor typeDescriptor in scalarTypeDescriptors.Values)
            {
                context.Register(typeDescriptor.Name, typeDescriptor);
            }
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
                            new NamedTypeDescriptor(
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
                            new NamedTypeDescriptor(
                                outputType.Name,
                                context.Namespace,
                                outputType.Implements.Select(t => t.Name).ToList(),
                                kind: TypeKind.EntityType,
                                graphQLTypeName: outputType.Type.Name,
                                implementedBy: operation
                                    .GetImplementations(outputType)
                                    .Select(t => typeDescriptors[t.Name])
                                    .Select(t => t.NamedTypeDescriptor)
                                    .ToList()));

                        typeDescriptors.Add(outputType.Name, descriptorModel);
                    }
                }
            }
        }

        private static void AddProperties(
            ClientModel model,
            IMapperContext context,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors,
            Dictionary<NameString, NamedTypeDescriptor> scalarTypeDescriptors)
        {
            foreach (TypeDescriptorModel typeDescriptorModel in typeDescriptors.Values.ToList())
            {
                var properties = new List<PropertyDescriptor>();

                foreach (var field in typeDescriptorModel.TypeModel.Fields)
                {
                    NamedTypeDescriptor? fieldType;

                    if (field.Type.IsLeafType())
                    {
                        var leafType = (ILeafType)field.Type.NamedType();

                        if (!scalarTypeDescriptors.TryGetValue(leafType.Name, out fieldType))
                        {
                            string[] runtimeTypeName =
                                ((string)leafType.ContextData[RuntimeType]!).Split('.');

                            fieldType = new NamedTypeDescriptor(
                                runtimeTypeName[^1],
                                string.Join(".", runtimeTypeName.Take(runtimeTypeName.Length - 1)),
                                graphQLTypeName: leafType.Name,
                                kind: TypeKind.LeafType);

                            scalarTypeDescriptors.Add(runtimeTypeName[^1], fieldType);
                        }
                    }
                    else
                    {
                        fieldType = GetFieldTypeDescriptor(
                            model, field.SyntaxNode, typeDescriptors);
                    }

                    properties.Add(
                        new PropertyDescriptor(
                            field.Name,
                            BuildFieldType(field.Type, fieldType)));
                }

                typeDescriptorModel.NamedTypeDescriptor.Complete(properties);
            }
        }

        private static NamedTypeDescriptor GetFieldTypeDescriptor(
            ClientModel model,
            FieldNode fieldSyntax,
            Dictionary<NameString, TypeDescriptorModel> typeDescriptors)
        {
            foreach (var operation in model.Operations)
            {
                if (operation.TryGetFieldResultType(fieldSyntax, out OutputTypeModel? fieldType))
                {
                    return typeDescriptors.Values
                        .First(t => t.TypeModel == fieldType)
                        .NamedTypeDescriptor;
                }
            }

            throw new InvalidOperationException();
        }

        public static ITypeDescriptor BuildFieldType(
            this IType original,
            NamedTypeDescriptor namedTypeDescriptor)
        {
            if (original is NonNullType nnt)
            {
                return new NonNullTypeDescriptor(BuildFieldType(nnt.Type, namedTypeDescriptor));
            }

            if (original is ListType lt)
            {
                return new ListTypeDescriptor(BuildFieldType(lt.ElementType, namedTypeDescriptor));
            }

            if (original is INamedType)
            {
                return namedTypeDescriptor;
            }

            throw new NotSupportedException();
        }

        private readonly struct TypeDescriptorModel
        {
            public TypeDescriptorModel(OutputTypeModel typeModel, NamedTypeDescriptor namedTypeDescriptor)
            {
                TypeModel = typeModel;
                NamedTypeDescriptor = namedTypeDescriptor;
            }

            public OutputTypeModel TypeModel { get; }

            public NamedTypeDescriptor NamedTypeDescriptor { get; }
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
                // yield return new EntityTypeDescriptor
            }

            yield break;
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
                        out EntityModel? entityModel))
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
