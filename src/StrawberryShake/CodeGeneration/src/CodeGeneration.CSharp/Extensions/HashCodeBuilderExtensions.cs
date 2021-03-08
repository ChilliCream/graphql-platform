using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    internal static class HashCodeBuilderExtensions
    {
        public static HashCodeBuilder AddProperties(
            this HashCodeBuilder hashCodeBuilder,
            IEnumerable<PropertyDescriptor> properties)
        {
            foreach (var property in properties)
            {
                hashCodeBuilder.AddCode(BuildProperty(property.Type, property.Name));
            }

            return hashCodeBuilder;
        }

        public static HashCodeBuilder AddProperty(
            this HashCodeBuilder hashCodeBuilder,
            PropertyDescriptor descriptor)
        {
            return hashCodeBuilder.AddCode(BuildProperty(descriptor.Type, descriptor.Name));
        }

        private static ICode BuildProperty(
            ITypeDescriptor type,
            string propertyName)
        {
            return BuildPropertyInternal(type, propertyName, true);

            ICode BuildPropertyInternal(
                ITypeDescriptor currentType,
                string variableName,
                bool isNullable)
            {
                ICode check = currentType switch
                {
                    NonNullTypeDescriptor d =>
                        BuildPropertyInternal(d.InnerType, variableName, false),
                    INamedTypeDescriptor => AssignmentBuilder
                        .New()
                        .SetLefthandSide(HashCodeBuilder.VariableName)
                        .SetOperator("^=")
                        .SetRighthandSide(MethodCallBuilder
                            .Inline()
                            .SetPrefix($"{HashCodeBuilder.Prime} * ")
                            .SetMethodName(variableName, nameof(GetHashCode))),
                    ListTypeDescriptor d => ForEachBuilder
                        .New()
                        .SetLoopHeader($"var {variableName}_elm in {variableName}")
                        .AddCode(BuildPropertyInternal(d.InnerType, variableName + "_elm", true)),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (isNullable && currentType is not NonNullTypeDescriptor)
                {
                    return IfBuilder
                        .New()
                        .SetCondition($"!({variableName} is null)")
                        .AddCode(check);
                }

                return check;
            }
        }
    }
}
