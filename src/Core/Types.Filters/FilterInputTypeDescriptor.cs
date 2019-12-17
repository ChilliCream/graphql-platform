using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
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
            if (entityType is null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            IFilterNamingConvention convention = context.GetFilterNamingConvention();

            Definition.EntityType = entityType;
            Definition.Name = convention.GetFilterTypeName(context, entityType);
            Definition.ClrType = typeof(object);
            // TODO : should we rework get type description?
            Definition.Description = context.Naming.GetTypeDescription(
                entityType, TypeKind.Object);
            Definition.Fields.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        internal protected override FilterInputTypeDefinition Definition { get; } =
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

        protected override void OnCreateDefinition(
            FilterInputTypeDefinition definition)
        {
            if (Definition.EntityType is { })
            {
                Context.Inspector.ApplyAttributes(this, Definition.EntityType);
            }

            var fields = new Dictionary<NameString, FilterOperationDefintion>();
            var handledProperties = new HashSet<PropertyInfo>();

            List<FilterFieldDefintion> explicitFields =
                Fields.Select(t => t.CreateDefinition()).ToList();

            FieldDescriptorUtilities.AddExplicitFields(
                explicitFields.Where(t => !t.Ignore).SelectMany(t => t.Filters),
                f => f.Operation.Property,
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
                    if (!handledProperties.Contains(property))
                    {
                        if (TryCreateImplicitFilter(property,
                            out FilterFieldDefintion definition))
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
        }

        private bool TryCreateImplicitFilter(
            PropertyInfo property,
            out FilterFieldDefintion definition)
        {
            Type type = property.PropertyType;

            if (type.IsGenericType
                && System.Nullable.GetUnderlyingType(type) is Type nullableType)
            {
                type = nullableType;
            }

            if (type == typeof(string))
            {
                var field = new StringFilterFieldDescriptor(Context, property);
                definition = field.CreateDefinition();
                return true;
            }

            if (type == typeof(bool))
            {
                var field = new BooleanFilterFieldDescriptor(
                    Context, property);
                definition = field.CreateDefinition();
                return true;
            }

            if (IsComparable(property.PropertyType))
            {
                var field = new ComparableFilterFieldDescriptor(
                    Context, property);
                definition = field.CreateDefinition();
                return true;
            }

            if (DotNetTypeInfoFactory.IsListType(type))
            {
                if (!TypeInspector.Default.TryCreate(type, out Utilities.TypeInfo typeInfo))
                {
                    throw new ArgumentException(
                        FilterResources.FilterArrayFieldDescriptor_InvalidType,
                        nameof(property));
                }

                Type elementType = typeInfo.ClrType;
                ArrayFilterFieldDescriptor field;

                if (elementType == typeof(string)
                    || elementType == typeof(bool)
                    || typeof(IComparable).IsAssignableFrom(elementType))
                {
                    field = new ArrayFilterFieldDescriptor(
                        Context,
                        property,
                        typeof(ISingleFilter<>).MakeGenericType(elementType));
                }
                else
                {
                    field = new ArrayFilterFieldDescriptor(Context, property, elementType);
                }

                definition = field.CreateDefinition();
                return true;
            }

            if (type.IsClass)
            {
                var field = new ObjectFilterFieldDescriptor(
                    Context, property, property.PropertyType);
                definition = field.CreateDefinition();
                return true;
            }

            definition = null;
            return false;
        }

        private bool IsComparable(Type type)
        {
            if (typeof(IComparable).IsAssignableFrom(type))
            {
                return true;
            }

            if (type.IsValueType
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return typeof(IComparable).IsAssignableFrom(
                    System.Nullable.GetUnderlyingType(type));
            }

            return false;
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
    }
}
