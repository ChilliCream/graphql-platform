using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterFieldDescriptorBase
        : ArgumentDescriptorBase<InputFieldDefinition>
    {
        protected FilterFieldDescriptorBase(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            Definition.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            Definition.Name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                property, MemberKind.InputObjectField);
            Definition.Type = context.Inspector.GetInputReturnType(property);
        }
    }
}
