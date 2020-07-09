using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDescriptor
        : DescriptorBase<FilterInputTypeDefinition>
        , IFilterInputTypeDescriptor
    {
        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType)
            : base(context)
        {
            Convention = context.GetFilterConvention();
            Definition.EntityType = entityType ??
                throw new ArgumentNullException(nameof(entityType));
            Definition.Name = Convention.GetTypeName(context, entityType);
            Definition.Description = Convention.GetTypeDescription(context, entityType);
            Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        }

        protected FilterInputTypeDescriptor(IDescriptorContext context)
            : base(context)
        {
            Convention = context.GetFilterConvention();
            Definition.EntityType = typeof(object);
        }

        internal protected sealed override FilterInputTypeDefinition Definition { get; } =
            new FilterInputTypeDefinition();

        protected BindableList<FilterFieldDescriptor> Fields { get; } =
            new BindableList<FilterFieldDescriptor>();

        protected BindableList<FilterOperationFieldDescriptor> Operations { get; } =
            new BindableList<FilterOperationFieldDescriptor>();
        protected IFilterConvention Convention { get; }

        protected override void OnCreateDefinition(
            FilterInputTypeDefinition definition)
        {
            if (Definition.EntityType is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.EntityType);
            }

            var fields = new Dictionary<NameString, InputFieldDefinition>();
            var handledProperties = new HashSet<PropertyInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition())
                    .Concat(Operations.Select(t => t.CreateDefinition())),
                f => f.Property,
                fields,
                handledProperties);

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, InputFieldDefinition> fields,
            ISet<PropertyInfo> handledProperties)
        {
        }

        public IFilterInputTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IFilterInputTypeDescriptor Description(
            string value)
        {
            Definition.Description = value;
            return this;
        }

        public IFilterInputTypeDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IFilterInputTypeDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective());
            return this;
        }

        public IFilterInputTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public IFilterInputTypeDescriptor BindFields(
            BindingBehavior bindingBehavior)
        {
            Definition.Fields.BindingBehavior = bindingBehavior;
            return this;
        }

        public IFilterInputTypeDescriptor BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public IFilterInputTypeDescriptor BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public IFilterOperationFieldDescriptor Operation(int operation)
        {
            FilterOperationFieldDescriptor fieldDescriptor =
                Operations.FirstOrDefault(t => t.Definition.Operation == operation);

            if (fieldDescriptor is { })
            {
                return fieldDescriptor;
            }

            fieldDescriptor = FilterOperationFieldDescriptor.New(Context, operation);

            Operations.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        public IFilterFieldDescriptor Field(NameString name)
        {
            FilterFieldDescriptor fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Name == name);

            if (fieldDescriptor is { })
            {
                return fieldDescriptor;
            }

            fieldDescriptor = FilterFieldDescriptor.New(Context, name);

            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        public static FilterInputTypeDescriptor New(
            IDescriptorContext context,
            Type entityType)
            => new FilterInputTypeDescriptor(context, entityType);

    }
}