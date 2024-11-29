using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class DataTypeDescriptorMapper
{
    public static void Map(ClientModel model, IMapperContext context)
    {
        context.Register(CollectDataTypes(model, context));
    }

    private static IEnumerable<DataTypeDescriptor> CollectDataTypes(
        ClientModel model,
        IMapperContext context)
    {
        var dataTypes = context.Types
            .OfType<ObjectTypeDescriptor>()
            .Where(x => x.IsData())
            .ToList();

        var unionTypes = model.Schema.Types
            .OfType<UnionType>()
            .ToList();

        var dataTypeInfos = new Dictionary<string, DataTypeInfo>(StringComparer.Ordinal);

        foreach (var dataType in dataTypes)
        {
            var objectType = model.Schema.GetType<ObjectType>(dataType.Name);

            var abstractTypes = new List<INamedType>();
            abstractTypes.AddRange(unionTypes.Where(t => t.ContainsType(dataType.Name)));
            abstractTypes.AddRange(objectType.Implements);

            if (!dataTypeInfos.TryGetValue(dataType.Name, out var dataTypeInfo))
            {
                dataTypeInfo = new DataTypeInfo(dataType.Name, dataType.Description);
                dataTypeInfo.AbstractTypeParentName.AddRange(
                    abstractTypes.Select(abstractType => abstractType.Name));
                dataTypeInfos.Add(dataType.Name, dataTypeInfo);
            }

            dataTypeInfo.Components.Add(dataType.RuntimeType.Name);
        }

        var handledAbstractTypes = new HashSet<string>();

        foreach (var dataTypeInfo in dataTypeInfos.Values)
        {
            var implements = new List<string>();

            foreach (var abstractTypeName in dataTypeInfo.AbstractTypeParentName)
            {
                var dataTypeInterfaceName = GetInterfaceName(abstractTypeName);
                implements.Add(dataTypeInterfaceName);
                if (handledAbstractTypes.Add(dataTypeInterfaceName))
                {
                    yield return new DataTypeDescriptor(
                        dataTypeInterfaceName,
                        NamingConventions.CreateStateNamespace(context.Namespace),
                        Array.Empty<ComplexTypeDescriptor>(),
                        Array.Empty<string>(),
                        dataTypeInfo.Description,
                        true);
                }
            }

            yield return new DataTypeDescriptor(
                dataTypeInfo.Name,
                NamingConventions.CreateStateNamespace(context.Namespace),
                dataTypeInfo.Components
                    .Select(name => context.Types.Single(
                        t => t.RuntimeType.Name.Equals(name)))
                    .OfType<ComplexTypeDescriptor>()
                    .ToList(),
                implements,
                dataTypeInfo.Description);
        }
    }

    private sealed class DataTypeInfo
    {
        public DataTypeInfo(string name, string? description)
        {
            Name = name;
            Components = [];
            AbstractTypeParentName = [];
            Description = description;
        }

        public string Name { get; }

        public string? Description { get; }

        public HashSet<string> Components { get; }

        public List<string> AbstractTypeParentName { get; }
    }
}
