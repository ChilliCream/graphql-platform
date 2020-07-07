using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDescriptor<T>
        : DescriptorBase<FilterInputTypeDefinition>
        , IFilterInputTypeDescriptor<T>
    {
        private readonly IFilterConvention _convention;

        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType,
            IFilterConvention convention)
            : base(context)
        {
            _convention = convention;

            Definition.EntityType = entityType ??
                throw new ArgumentNullException(nameof(entityType));
            Definition.Name = _convention.GetTypeName(entityType);
            Definition.Description = _convention.GetTypeDescription(entityType);
            Definition.Fields.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        protected sealed override FilterInputTypeDefinition Definition { get; } =
            new FilterInputTypeDefinition();

        protected BindableList<IFilterFieldDescriptorBase> Fields { get; } =
            new BindableList<IFilterFieldDescriptorBase>();

        public IFilterInputTypeDescriptor<T> Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IFilterInputTypeDescriptor<T> Description(
            string value)
        {
            Definition.Description = value;
            return this;
        }

        public IFilterInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective());
            return this;
        }

        public IFilterInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

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
                Fields.Select(t => t.CreateFieldDefinition()),
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
            if (Definition.Fields.IsImplicitBinding()
                && Definition.EntityType != typeof(object))
            {
                foreach (PropertyInfo property in Context.Inspector
                    .GetMembers(Definition.EntityType)
                    .OfType<PropertyInfo>())
                {
                    if (!handledProperties.Contains(property)
                        && _convention.TryCreateImplicitFilter(property,
                            out InputFieldDefinition? definition)
                        && !fields.ContainsKey(definition.Name))
                    {
                        fields[definition.Name] = definition;
                    }
                }
            }
        }


        public IFilterInputTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior)
        {
            Definition.Fields.BindingBehavior = bindingBehavior;
            return this;
        }

        public IFilterInputTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public IFilterInputTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context,
            Type entityType,
            IFilterConvention convention) =>
                new FilterInputTypeDescriptor<T>(context, entityType, convention);

        public IFilterOperationFieldDescriptor Operation(int operation)
        {
            throw new NotImplementedException();
        }

        public IFilterOperationFieldDescriptor Operation<TField>(Expression<Func<T, TField>> property)
        {
            throw new NotImplementedException();
        }

        public IFilterFieldDescriptor Field<TField>(Expression<Func<T, TField>> property)
        {
            throw new NotImplementedException();
        }
    }

}
