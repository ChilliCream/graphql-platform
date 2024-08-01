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
        var entityTypes = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
        var descriptions = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var operation in model.Operations)
        {
            foreach (var outputType in
                operation.OutputTypes.Where(t => !t.IsInterface && !t.IsFragment))
            {
                var namedType = outputType.Type.NamedType();
                descriptions[namedType.Name] = outputType.Description;
                if (namedType.IsEntity())
                {
                    if (!entityTypes.TryGetValue(
                        namedType.Name,
                        out var components))
                    {
                        components = new Dictionary<string, bool>(StringComparer.Ordinal);
                        entityTypes.Add(namedType.Name, components);
                    }

                    if (!components.ContainsKey(outputType.Name))
                    {
                        components.Add(outputType.Name, false);
                    }

                    if (outputType.Deferred.Count > 0)
                    {
                        foreach (var deferred in outputType.Deferred.Values)
                        {
                            components[deferred.Class.Name] = true;
                        }
                    }
                }
            }
        }

        foreach (var (key, value) in entityTypes)
        {
            var runtimeType = CreateEntityType(key, context.Namespace);
            descriptions.TryGetValue(key, out var description);
            var properties = new Dictionary<string, PropertyDescriptor>();

            var entityTypeDescriptor = new EntityTypeDescriptor(
                key,
                runtimeType,
                properties,
                description);

            foreach (var (typeName, isFragment) in value.OrderBy(t => t.Value))
            {
                var type = context.GetType<ComplexTypeDescriptor>(typeName);

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

                foreach (var property in type.Properties)
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
