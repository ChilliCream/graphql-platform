using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;
using HotChocolate.Types.Descriptors;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters
{
    public class FilterInputObjectTypeDescriptor<T>
        : DescriptorBase<InputObjectTypeDefinition>
        , IFilterInputObjectTypeDescriptor<T>
    {
        public FilterInputObjectTypeDescriptor(
            IDescriptorContext context,
            Type clrType)
            : base(context)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(
                clrType, TypeKind.InputObject);
            Definition.Description = context.Naming.GetTypeDescription(
                clrType, TypeKind.InputObject);
        }

        protected override InputObjectTypeDefinition Definition { get; } =
            new InputObjectTypeDefinition();

        protected List<FilterFieldDescriptorBase> Fields { get; } =
            new List<FilterFieldDescriptorBase>();

        protected override void OnCreateDefinition(
            InputObjectTypeDefinition definition)
        {
            var fields = new Dictionary<NameString, InputFieldDefinition>();
            var handledProperties = new HashSet<PropertyInfo>();

            /*
            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition()),
                f => f.Property,
                fields,
                handledProperties);
            */

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, InputFieldDefinition> fields,
            ISet<PropertyInfo> handledProperties)
        {
        }

        public IStringFilterFieldsDescriptor Filter(
            Expression<Func<T, string>> propertyOrMethod)
        {
            throw new NotImplementedException();
        }
    }
}
