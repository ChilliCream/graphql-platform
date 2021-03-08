using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Mappers
{
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
                .Where(x => x.IsDataType())
                .ToList();

            var unionTypes = model.Schema.Types
                .OfType<UnionType>()
                .ToList();

            var dataTypeInfos = new Dictionary<NameString, DataTypeInfo>();

            foreach (ObjectTypeDescriptor dataType in dataTypes)
            {
                ObjectType objectType = model.Schema.GetType<ObjectType>(dataType.Name);

                var abstractTypes = new List<INamedType>();
                abstractTypes.AddRange(unionTypes.Where(t => t.ContainsType(dataType.Name)));
                abstractTypes.AddRange(objectType.Implements);

                if (!dataTypeInfos.TryGetValue(dataType.Name, out var dataTypeInfo))
                {
                    dataTypeInfo = new DataTypeInfo(dataType.Name, dataType.Description);
                    dataTypeInfo.AbstractTypeParentName.AddRange(
                        abstractTypes.Select(abstractType => abstractType.Name.Value));
                    dataTypeInfos.Add(dataType.Name, dataTypeInfo);
                }

                dataTypeInfo.Components.Add(dataType.RuntimeType.Name);
            }

            var handledAbstractTypes = new HashSet<string>();

            foreach (DataTypeInfo dataTypeInfo in dataTypeInfos.Values)
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
                        .Select(name => context.Types.Single(t => t.RuntimeType.Name.Equals(name)))
                        .OfType<ComplexTypeDescriptor>()
                        .ToList(),
                    implements,
                    dataTypeInfo.Description);
            }
        }

        private class DataTypeInfo
        {
            public DataTypeInfo(NameString name, string? description)
            {
                Name = name;
                Components = new HashSet<NameString>();
                AbstractTypeParentName = new List<string>();
                Description = description;
            }

            public NameString Name { get; }

            public string? Description { get; }

            public HashSet<NameString> Components { get; }

            public List<string> AbstractTypeParentName { get; }
        }
    }
}
