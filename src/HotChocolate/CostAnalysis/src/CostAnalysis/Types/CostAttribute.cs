using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.CostAnalysis.Types;

/// <summary>
/// Applies the <c>@cost</c> directive. The purpose of the <c>cost</c> directive is to define a
/// <c>weight</c> for GraphQL types, fields, and arguments. Static analysis can use these weights
/// when calculating the overall cost of a query or response.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Enum
    | AttributeTargets.Method
    | AttributeTargets.Parameter
    | AttributeTargets.Property
    | AttributeTargets.Struct)]
public sealed class CostAttribute : DescriptorAttribute
{
    private readonly double _weight;

    /// <summary>
    /// Initializes a new instance of <see cref="CostAttribute"/>.
    /// </summary>
    /// <param name="weight">
    /// The <c>weight</c> argument defines what value to add to the overall cost for every
    /// appearance, or possible appearance, of a type, field, argument, etc.
    /// </param>
    public CostAttribute(double weight)
    {
        _weight = weight;
    }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IArgumentDescriptor argumentDescriptor:
                argumentDescriptor.Directive(new CostDirective(_weight));
                break;

            case IEnumTypeDescriptor enumTypeDescriptor:
                enumTypeDescriptor.Directive(new CostDirective(_weight));
                break;

            case IInputFieldDescriptor inputFieldDescriptor:
                inputFieldDescriptor.Directive(new CostDirective(_weight));
                break;

            case IObjectFieldDescriptor objectFieldDescriptor:
                objectFieldDescriptor.Directive(new CostDirective(_weight));
                break;

            case IObjectTypeDescriptor objectTypeDescriptor:
                objectTypeDescriptor.Directive(new CostDirective(_weight));
                break;

            case IScalarTypeDescriptor scalarTypeDescriptor:
                scalarTypeDescriptor.Directive(new CostDirective(_weight));
                break;
        }
    }
}
