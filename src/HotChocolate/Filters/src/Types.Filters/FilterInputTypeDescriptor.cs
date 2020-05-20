using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Filters.Extensions;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeDescriptor
        : DescriptorBase<FilterInputTypeDefinition>
        , IFilterInputTypeDescriptor
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
            Definition.ClrType = typeof(object);
            Definition.Name = _convention.GetTypeName(context, entityType);
            Definition.Description = _convention.GetTypeDescription(context, entityType);
            Definition.Fields.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        protected internal sealed override FilterInputTypeDefinition Definition { get; } =
            new FilterInputTypeDefinition();

        protected List<FilterFieldDescriptorBase> Fields { get; } =
            new List<FilterFieldDescriptorBase>();

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
        public TDesc AddFilter<TDesc>(
            PropertyInfo property,
            Func<IDescriptorContext, TDesc> factory)
            where TDesc : FilterFieldDescriptorBase =>
                Fields.GetOrAddDescriptor(property, () => factory(Context));

        protected override void OnCreateDefinition(
            FilterInputTypeDefinition definition)
        {
            if (Definition.EntityType is { })
            {
                Context.Inspector.ApplyAttributes(Context, this, Definition.EntityType);
            }

            var fields = new Dictionary<NameString, FilterOperationDefintion>();
            var handledProperties = new HashSet<PropertyInfo>();

            var explicitFields = Fields.Select(t => t.CreateDefinition()).ToList();

            FieldDescriptorUtilities.AddExplicitFields(
                explicitFields.Where(t => !t.Ignore).SelectMany(t => t.Filters),
                f => f.Operation?.Property,
                fields,
                handledProperties);

            foreach (FilterFieldDefintion field in explicitFields.Where(t => t.Ignore))
            {
                handledProperties.Add(field.Property);
            }

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, FilterOperationDefintion> fields,
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
                        && TryCreateImplicitFilter(property,
                            out FilterFieldDefintion? definition))
                    {
                        foreach (FilterOperationDefintion filter in definition.Filters)
                        {
                            if (!fields.ContainsKey(filter.Name))
                            {
                                fields[filter.Name] = filter;
                            }
                        }
                    }
                }
            }
        }

        private bool TryCreateImplicitFilter(
            PropertyInfo property,
            [NotNullWhen(true)] out FilterFieldDefintion? definition)
        {
            definition = null;
            Type type = property.PropertyType;

            if (type.IsGenericType &&
                System.Nullable.GetUnderlyingType(type) is Type nullableType)
            {
                type = nullableType;
            }

            IEnumerator<TryCreateImplicitFilter> enumerator =
                _convention.GetImplicitFactories().GetEnumerator();

            while (enumerator.MoveNext()
                && !enumerator.Current(Context, type, property, _convention, out definition))
            {/**/}

            return definition != null;
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

        public static FilterInputTypeDescriptor New(
            IDescriptorContext context,
            Type entityType,
            IFilterConvention convention) =>
                new FilterInputTypeDescriptor(context, entityType, convention);
    }
}
