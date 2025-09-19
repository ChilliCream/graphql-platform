using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Descriptors;

public sealed class DataTypeDescriptor : ICodeDescriptor
{
    /// <summary>
    /// Describes the DataType
    /// </summary>
    /// <param name="name">
    ///
    /// </param>
    /// <param name="namespace">
    ///
    /// </param>
    /// <param name="operationTypes">
    /// The types that are subsets of the DataType represented by this descriptor.
    /// </param>
    /// <param name="implements"></param>
    /// <param name="documentation"></param>
    /// <param name="isInterface"></param>
    public DataTypeDescriptor(
        string name,
        string @namespace,
        IReadOnlyList<ComplexTypeDescriptor> operationTypes,
        IReadOnlyList<string> implements,
        string? documentation,
        bool isInterface = false)
    {
        var allProperties = new Dictionary<string, PropertyDescriptor>();

        foreach (var namedTypeReferenceDescriptor in
                 operationTypes.SelectMany(operationType => operationType.Properties))
        {
            if (!allProperties.ContainsKey(namedTypeReferenceDescriptor.Name))
            {
                if (namedTypeReferenceDescriptor.Type is NonNullTypeDescriptor nonNull)
                {
                    allProperties.Add(
                        namedTypeReferenceDescriptor.Name,
                        new PropertyDescriptor(
                            namedTypeReferenceDescriptor.Name,
                            namedTypeReferenceDescriptor.Name,
                            nonNull.InnerType,
                            namedTypeReferenceDescriptor.Description));
                }
                else
                {
                    allProperties.Add(
                        namedTypeReferenceDescriptor.Name,
                        namedTypeReferenceDescriptor);
                }
            }
        }

        Properties = allProperties.Select(pair => pair.Value).ToList();
        Name = name;
        RuntimeType = new(NamingConventions.CreateDataTypeName(name), @namespace);
        Implements = implements;
        IsInterface = isInterface;
        Documentation = documentation;
    }

    /// <summary>
    /// Gets the GraphQL type name which this entity represents.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public RuntimeTypeInfo RuntimeType { get; }

    /// <summary>
    /// Defines if this data type descriptor represents an interface.
    /// </summary>
    public bool IsInterface { get; }

    /// <summary>
    /// The documentation of this type
    /// </summary>
    public string? Documentation { get; }

    /// <summary>
    /// Gets the properties of this entity.
    /// </summary>
    public IReadOnlyList<PropertyDescriptor> Properties { get; }

    /// <summary>
    /// The interfaces that this data type implements. A data type does only implement
    /// interfaces, if it is part of a graphql union or interface type.
    /// </summary>
    public IReadOnlyList<string> Implements { get; }
}
