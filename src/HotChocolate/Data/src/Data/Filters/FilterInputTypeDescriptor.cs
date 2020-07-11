using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDescriptor<T>
        : FilterInputTypeDescriptor
        , IFilterInputTypeDescriptor<T>
    {
        protected FilterInputTypeDescriptor(IDescriptorContext context, Type entityType) : base(context, entityType)
        {
        }

        protected FilterInputTypeDescriptor(IDescriptorContext context) : base(context)
        {
        }

        public new IFilterInputTypeDescriptor<T> Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFields(BindingBehavior bindingBehavior)
        {
            base.BindFields(bindingBehavior);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFieldsExplicitly()
        {
            base.BindFieldsExplicitly();
            return this;
        }

        public new IFilterInputTypeDescriptor<T> BindFieldsImplicitly()
        {
            base.BindFieldsImplicitly();
            return this;
        }

        public IFilterFieldDescriptor Operation<TField>(Expression<Func<T, TField>> method)
        {
            if (method.ExtractMember() is MethodInfo m)
            {
                FilterFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor is { })
                {
                    return fieldDescriptor;
                }

                fieldDescriptor = FilterFieldDescriptor.New(Context, m);

                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "Only method are allowed for filter operation input types.",
                nameof(method));
        }

        public IFilterFieldDescriptor Field<TField>(Expression<Func<T, TField>> property)
        {
            if (property.ExtractMember() is PropertyInfo m)
            {
                FilterFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor is { })
                {
                    return fieldDescriptor;
                }

                fieldDescriptor = FilterFieldDescriptor.New(Context, m);

                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public IFilterInputTypeDescriptor<T> Ignore(Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo m)
            {
                FilterFieldDescriptor fieldDescriptor =
                    Fields.FirstOrDefault(t => t.Definition.Member == m);

                if (fieldDescriptor == null)
                {
                    fieldDescriptor = FilterFieldDescriptor.New(Context, m);

                    Fields.Add(fieldDescriptor);
                }

                fieldDescriptor.Ignore();

                return this;
            }

            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public new IFilterInputTypeDescriptor<T> Directive<TDirective>(TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Ignore(NameString name)
        {
            base.Ignore(name);
            return this;
        }

        public new IFilterInputTypeDescriptor<T> Ignore(int operation)
        {
            base.Ignore(operation);
            return this;
        }
    }

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

        public IFilterInputTypeDescriptor Ignore(NameString name)
        {
            FilterFieldDescriptor fieldDescriptor =
                Fields.FirstOrDefault(t => t.Definition.Name == name);

            if (fieldDescriptor == null)
            {
                fieldDescriptor = FilterFieldDescriptor.New(Context, name);
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
                fieldDescriptor = FilterOperationFieldDescriptor.New(Context, operation);
                Operations.Add(fieldDescriptor);
            }

            fieldDescriptor.Ignore();
            return this;
        }
    }
}