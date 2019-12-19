using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;
using HotChocolate.Types.Descriptors;
using System.Linq.Expressions;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeDescriptor<T>
        : DescriptorBase<FilterInputTypeDefinition>
        , IFilterInputTypeDescriptor<T>
    {

        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType)
            : base(context)
        {
            Definition.EntityType = entityType
                ?? throw new ArgumentNullException(nameof(entityType));
            Definition.ClrType = typeof(object);

            // TODO : should we rework get type name?
            Definition.Name = context.Naming.GetTypeName(
                entityType, TypeKind.Object) + "Filter";
            // TODO : should we rework get type description?
            Definition.Description = context.Naming.GetTypeDescription(
                entityType, TypeKind.Object);
            Definition.Fields.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        protected override FilterInputTypeDefinition Definition { get; } =
            new FilterInputTypeDefinition();

        protected List<FilterFieldDescriptorBase> Fields { get; } =
            new List<FilterFieldDescriptorBase>();


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

        public IFilterInputTypeDescriptor<T> Directive<TDirective>(TDirective directiveInstance)
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

            var fields = new Dictionary<NameString, FilterOperationDefintion>();
            var handledProperties = new HashSet<PropertyInfo>();

            List<FilterFieldDefintion> explicitFields =
                Fields.Select(t => t.CreateDefinition()).ToList();

            FieldDescriptorUtilities.AddExplicitFields(
                explicitFields.Where(t => !t.Ignore).SelectMany(t => t.Filters),
                f => f.Operation.Property,
                fields,
                handledProperties);

            foreach (var field in explicitFields.Where(t => t.Ignore))
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
                            foreach (FilterOperationDefintion filter in
                                definition.Filters)
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
            if (property.PropertyType == typeof(string))
            {
                var field = new StringFilterFieldDescriptor(Context, property);
                definition = field.CreateDefinition();
                return true;
            }

            if (property.PropertyType == typeof(bool)
                || property.PropertyType == typeof(bool?))
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
                    Nullable.GetUnderlyingType(type));
            }

            return false;
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

        public IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new StringFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }


        public IBooleanFilterFieldDescriptor Filter(
            Expression<Func<T, bool>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new BooleanFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }


        public IComparableFilterFieldDescriptor Filter(
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new ComparableFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public IFilterInputTypeDescriptor<T> Ignore(
            Expression<Func<T, object>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                Fields.Add(new IgnoredFilterFieldDescriptor(Context, p));
                return this;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context, Type entityType) =>
            new FilterInputTypeDescriptor<T>(context, entityType);
    }
}
