using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class DataTypeDescriptorMapper
    {
        public static void Map(
            ClientModel model,
            IMapperContext context)
        {
            foreach (var dataTypeDescriptor in CollectDataTypes(
                model,
                context))
            {
                context.Register(
                    dataTypeDescriptor.GraphQLTypeName,
                    dataTypeDescriptor);
            }
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
                    dataTypeInfo = new DataTypeInfo(dataType.Name);
                    dataTypeInfo.AbstractTypeParentName.AddRange(
                        abstractTypes.Select(abstractType => abstractType.Name.Value));
                    dataTypeInfos.Add(dataType.Name, dataTypeInfo);
                }

                dataTypeInfo.Components.Add(dataType.Name);
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
                            context.StateNamespace,
                            Array.Empty<ComplexTypeDescriptor>(),
                            Array.Empty<string>(),
                            true);
                    }
                }

                yield return new DataTypeDescriptor(
                    dataTypeInfo.Name,
                    context.StateNamespace,
                    dataTypeInfo.Components
                        .Select(name => context.Types.Single(t => t.Name.Equals(name)))
                        .OfType<ComplexTypeDescriptor>()
                        .ToList(),
                    implements);
            }
        }

        private class DataTypeInfo
        {
            public DataTypeInfo(NameString name)
            {
                Name = name;
                Components = new HashSet<NameString>();
                AbstractTypeParentName = new List<string>();
            }

            public NameString Name { get; }

            public HashSet<NameString> Components { get; }

            public List<string> AbstractTypeParentName { get; }
        }
    }
}
