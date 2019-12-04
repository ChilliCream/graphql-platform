using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

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
            if (entityType is null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            Definition.EntityType = entityType;
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

        internal protected override FilterInputTypeDefinition Definition { get; } =
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

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
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

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
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

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
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

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IObjectFilterFieldDescriptor<TObject> Object<TObject>(
            Expression<Func<T, TObject>> property) where TObject : class
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new ObjectFilterFieldDescriptor<TObject>(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IArrayFilterFieldDescriptor<TObject> ListFilter<TObject, TListType>(
            Expression<Func<T, TListType>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new ArrayFilterFieldDescriptor<TObject>(Context, p);
                Fields.Add(field);
                return field;
            }

            throw new ArgumentException(
                FilterResources.FilterInputTypeDescriptor_OnlyProperties,
                nameof(property));
        }

        public IArrayFilterFieldDescriptor<TObject> List<TObject>(
            Expression<Func<T, IEnumerable<TObject>>> property)
            where TObject : class
        {
            return ListFilter<TObject, IEnumerable<TObject>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<string>> List(
            Expression<Func<T, IEnumerable<string>>> property)
        {
            return ListFilter<ISingleFilter<string>, IEnumerable<string>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<bool>> List(
            Expression<Func<T, IEnumerable<bool>>> property)
        {
            return ListFilter<ISingleFilter<bool>, IEnumerable<bool>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<TStruct>(
            Expression<Func<T, IEnumerable<TStruct>>> property,
            IFilterInputTypeDescriptor<T>.RequireStruct<TStruct> ignore = null)
            where TStruct : struct
        {
            return ListFilter<ISingleFilter<TStruct>, IEnumerable<TStruct>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<TStruct>(
            Expression<Func<T, IEnumerable<TStruct?>>> property,
            IFilterInputTypeDescriptor<T>.RequireStruct<TStruct> ignore = null)
            where TStruct : struct
        {
            return ListFilter<ISingleFilter<TStruct>, IEnumerable<TStruct?>>(property);
        }

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context, Type entityType) =>
            new FilterInputTypeDescriptor<T>(context, entityType);
    }
}
