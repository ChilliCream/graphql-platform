using System;
using System.Collections.Generic;
using System.Linq;
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
            string? scope,
            Type entityType)
            : base(context)
        {
            Convention = context.GetFilterConvention(scope);
            Definition.EntityType = entityType ??
                throw new ArgumentNullException(nameof(entityType));
            Definition.RuntimeType = entityType;
            Definition.Name = Convention.GetTypeName(context, entityType);
            Definition.Description = Convention.GetTypeDescription(context, entityType);
            Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
            Definition.Scope = scope;
        }

        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            string? scope)
            : base(context)
        {
            Convention = context.GetFilterConvention(scope);
            Definition.RuntimeType = typeof(object);
            Definition.EntityType = typeof(object);
            Definition.Scope = scope;
        }

        protected internal FilterInputTypeDescriptor(
            IDescriptorContext context,
            FilterInputTypeDefinition definition,
            string? scope)
            : base(context)
        {
            Convention = context.GetFilterConvention(scope);
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        protected BindableList<FilterFieldDescriptor> Fields { get; } =
            new BindableList<FilterFieldDescriptor>();

        protected BindableList<FilterOperationFieldDescriptor> Operations { get; } =
            new BindableList<FilterOperationFieldDescriptor>();

        protected IFilterConvention Convention { get; }

        protected internal override FilterInputTypeDefinition Definition { get; protected set; } =
            new FilterInputTypeDefinition();

        Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

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

            fieldDescriptor = FilterOperationFieldDescriptor.New(Context, Definition.Scope, operation);

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

            fieldDescriptor = FilterFieldDescriptor.New(Context, Definition.Scope, name);

            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        public IFilterInputTypeDescriptor Ignore(NameString name)
        {
            FilterFieldDescriptor fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Name == name);

            if (fieldDescriptor == null)
            {
                fieldDescriptor = FilterFieldDescriptor.New(Context, Definition.Scope, name);
                Fields.Add(fieldDescriptor);
            }

            fieldDescriptor.Ignore();
            return this;
        }

        public IFilterInputTypeDescriptor Ignore(int operation)
        {
            FilterOperationFieldDescriptor fieldDescriptor =
                Operations.FirstOrDefault(t => t.Definition.Operation == operation);

            if (fieldDescriptor == null)
            {
                fieldDescriptor = FilterOperationFieldDescriptor.New(
                    Context, Definition.Scope, operation);

                Operations.Add(fieldDescriptor);
            }

            fieldDescriptor.Ignore();
            return this;
        }

        public IFilterInputTypeDescriptor UseOr(bool isUsed = true)
        {
            Definition.UseOr = isUsed;
            return this;
        }

        public IFilterInputTypeDescriptor UseAnd(bool isUsed = true)
        {
            Definition.UseAnd = isUsed;
            return this;
        }

        public static FilterInputTypeDescriptor New(
            IDescriptorContext context,
            string? scope,
            Type entityType) =>
            new FilterInputTypeDescriptor(context, scope, entityType);

        public static FilterInputTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            string? scope,
            Type schemaType)
        {
            FilterInputTypeDescriptor? descriptor = New(context, scope, schemaType);
            descriptor.Definition.RuntimeType = typeof(object);
            return descriptor;
        }

        public static FilterInputTypeDescriptor From(
            IDescriptorContext context,
            FilterInputTypeDefinition definition,
            string? scope) =>
            new FilterInputTypeDescriptor(context, definition, scope);

        public static FilterInputTypeDescriptor<T> From<T>(
            IDescriptorContext context,
            FilterInputTypeDefinition definition,
            string? scope) =>
            new FilterInputTypeDescriptor<T>(context, definition, scope);
    }
}