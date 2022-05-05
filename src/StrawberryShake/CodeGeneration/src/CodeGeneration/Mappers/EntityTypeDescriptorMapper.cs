using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class EntityTypeDescriptorMapper
{
    private static readonly ScalarTypeDescriptor _boolean = new(
        "Boolean",
        new RuntimeTypeInfo(TypeNames.Boolean, true),
        new RuntimeTypeInfo(TypeNames.Boolean, true));

    public static void Map(ClientModel model, IMapperContext context)
    {
        context.Register(CollectEntityTypes(model, context));
    }

    private static IEnumerable<EntityTypeDescriptor> CollectEntityTypes(
        ClientModel model,
        IMapperContext context)
    {
        var entityTypes = new Dictionary<NameString, Dictionary<NameString, bool>>();
        var descriptions = new Dictionary<NameString, string?>();

        foreach (OperationModel operation in model.Operations)
        {
            foreach (OutputTypeModel outputType in
                operation.OutputTypes.Where(t => !t.IsInterface && !t.IsFragment))
            {
                INamedType namedType = outputType.Type.NamedType();
                descriptions[namedType.Name] = outputType.Description;
                if (namedType.IsEntity())
                {
                    if (!entityTypes.TryGetValue(
                        namedType.Name,
                        out Dictionary<NameString, bool>? components))
                    {
                        components = new Dictionary<NameString, bool>();
                        entityTypes.Add(namedType.Name, components);
                    }

                    if (!components.ContainsKey(outputType.Name))
                    {
                        components.Add(outputType.Name, false);
                    }

                    if (outputType.Deferred.Count > 0)
                    {
                        foreach (DeferredFragmentModel deferred in outputType.Deferred.Values)
                        {
                            components[deferred.Class.Name] = true;
                        }
                    }
                }
            }
        }

        foreach ((NameString key, Dictionary<NameString, bool>? value) in entityTypes)
        {
            RuntimeTypeInfo runtimeType = CreateEntityType(key, context.Namespace);
            descriptions.TryGetValue(key, out var description);
            var properties = new Dictionary<string, PropertyDescriptor>();

            var entityTypeDescriptor = new EntityTypeDescriptor(
                key,
                runtimeType,
                properties,
                description);

            foreach ((NameString typeName, var isFragment) in value.OrderBy(t => t.Value))
            {
                ComplexTypeDescriptor type = context.GetType<ComplexTypeDescriptor>(typeName);

                if (isFragment)
                {
                    var indicator = new PropertyDescriptor(
                        $"Is{typeName}Fulfilled",
                        $"_is{typeName}Fulfilled",
                        new NonNullTypeDescriptor(_boolean),
                        null,
                        PropertyKind.FragmentIndicator);
                    properties.Add(indicator.Name, indicator);
                }

                foreach (PropertyDescriptor property in type.Properties)
                {
                    if (!properties.ContainsKey(property.Name))
                    {
                        properties.Add(property.Name, property);
                    }
                }

                if (type is ObjectTypeDescriptor objectType)
                {
                    objectType.CompleteEntityType(entityTypeDescriptor);
                }
            }

            yield return entityTypeDescriptor;
        }
    }
}
