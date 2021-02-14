using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;

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

        public static IEnumerable<DataTypeDescriptor> CollectDataTypes(
            ClientModel model,
            IMapperContext context)
        {
            {
                var dataTypes = new Dictionary<NameString,
                    (HashSet<NameString> components, List<string> abstractTypeParentName)>();

                var allDataTypes = context.Types
                    .Where(x => x.IsDataType() && !x.IsInterface)
                    .ToList();

                var unionTypes = model.Schema.Types.OfType<UnionType>().ToList();
                foreach (NamedTypeDescriptor namedTypeDescriptor in allDataTypes)
                {
                    var graphQlTypeName = namedTypeDescriptor.GraphQLTypeName ??
                                          throw new ArgumentNullException();

                    var graphQlType =
                        model.Schema.GetType<ObjectType>(graphQlTypeName);

                    var abstractTypes = new List<INamedType>();
                    abstractTypes.AddRange(
                        unionTypes.Where(type => type.ContainsType(graphQlTypeName)));
                    abstractTypes.AddRange(graphQlType.Implements);

                    if (!dataTypes.TryGetValue(
                        graphQlTypeName,
                        out (HashSet<NameString>?, List<string>) data))
                    {
                        data.Item1 = new HashSet<NameString>();
                        data.Item2 = new List<string>();
                        data.Item2.AddRange(
                            abstractTypes.Select(abstractType => abstractType.Name.Value));
                        dataTypes.Add(
                            graphQlTypeName,
                            (data.Item1, data.Item2));
                    }

                    data.Item1.Add(namedTypeDescriptor.Name);
                }

                var handledAbstractTypes = new HashSet<string>();

                foreach (KeyValuePair<NameString, (HashSet<NameString>, List<string>)> dataType in
                    dataTypes)
                {
                    var implements = new List<string>();
                    if (dataType.Value.Item2 is not null)
                    {
                        foreach (var abstractTypeName in dataType.Value.Item2)
                        {
                            var dataTypeInterfaceName = "I" + abstractTypeName;
                            implements.Add(dataTypeInterfaceName);
                            if (handledAbstractTypes.Add(dataTypeInterfaceName))
                            {
                                yield return new DataTypeDescriptor(
                                    dataTypeInterfaceName,
                                    context.StateNamespace,
                                    Array.Empty<NamedTypeDescriptor>(),
                                    Array.Empty<string>(),
                                    true);
                            }
                        }
                    }

                    yield return new DataTypeDescriptor(
                        dataType.Key,
                        context.StateNamespace,
                        dataType.Value.Item1
                            .Select(name => context.Types.Single(t => t.Name.Equals(name)))
                            .ToList(),
                        implements);
                }
            }
        }
    }
}
