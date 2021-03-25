using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters.Extensions;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class FilterInputTypeDescriptor<T>
        : DescriptorBase<FilterInputTypeDefinition>
        , IFilterInputTypeDescriptor<T>
    {
        protected FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type entityType)
            : base(context)
        {
            IFilterNamingConvention convention = context.GetFilterNamingConvention();
            Definition.EntityType = entityType
                ?? throw new ArgumentNullException(nameof(entityType));
            Definition.RuntimeType = typeof(object);
            Definition.Name = convention.GetFilterTypeName(context, entityType);
            // TODO : should we rework get type description?
            Definition.Description = context.Naming.GetTypeDescription(
                entityType,
                TypeKind.Object);
            Definition.Fields.BindingBehavior =
                context.Options.DefaultBindingBehavior;
        }

        protected internal sealed override FilterInputTypeDefinition Definition
        {
            get;
            protected set;
        } = new FilterInputTypeDefinition();

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
            Definition.AddDirective(directiveInstance, Context.TypeInspector);
            return this;
        }

        public IFilterInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective(), Context.TypeInspector);
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
            if (!Definition.AttributesAreApplied && Definition.EntityType is not null)
            {
                Context.TypeInspector.ApplyAttributes(Context, this, Definition.EntityType);
                Definition.AttributesAreApplied = true;
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
                foreach (PropertyInfo property in Context.TypeInspector
                    .GetMembers(Definition.EntityType)
                    .OfType<PropertyInfo>())
                {
                    if (!handledProperties.Contains(property)
                        && TryCreateImplicitFilter(
                            property,
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
            Type type = property.PropertyType;

            if (type.IsGenericType
                && System.Nullable.GetUnderlyingType(type) is { } nullableType)
            {
                type = nullableType;
            }

            if (type == typeof(string))
            {
                var field = new StringFilterFieldDescriptor(Context, property);
                field.BindFilters(Definition.Fields.BindingBehavior);
                definition = field.CreateDefinition();
                return true;
            }

            if (type == typeof(bool))
            {
                var field = new BooleanFilterFieldDescriptor(
                    Context,
                    property);
                field.BindFilters(Definition.Fields.BindingBehavior);
                definition = field.CreateDefinition();
                return true;
            }

            if (IsComparable(property.PropertyType))
            {
                var field = new ComparableFilterFieldDescriptor(
                    Context,
                    property);
                field.BindFilters(Definition.Fields.BindingBehavior);
                definition = field.CreateDefinition();
                return true;
            }

            if (Context.TypeInspector.TryCreateTypeInfo(type, out ITypeInfo? typeInfo) &&
                typeInfo.GetExtendedType()?.ElementType?.Source is { } elementType)
            {
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

                field.BindFilters(Definition.Fields.BindingBehavior);
                definition = field.CreateDefinition();
                return true;
            }

            if (type.IsClass)
            {
                var field = new ObjectFilterFieldDescriptor(
                    Context,
                    property,
                    property.PropertyType);
                field.BindFilters(Definition.Fields.BindingBehavior);
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
                return Fields.GetOrAddDescriptor(
                    p,
                    () => new StringFilterFieldDescriptor(Context, p));
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
                return Fields.GetOrAddDescriptor(
                    p,
                    () => new BooleanFilterFieldDescriptor(Context, p));
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
                return Fields.GetOrAddDescriptor(
                    p,
                    () => new ComparableFilterFieldDescriptor(Context, p));
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
                Fields.GetOrAddDescriptor(
                    p,
                    () => new IgnoredFilterFieldDescriptor(Context, p));
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
                return Fields.GetOrAddDescriptor(
                    p,
                    () => new ObjectFilterFieldDescriptor<TObject>(Context, p));
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
                return Fields.GetOrAddDescriptor(
                    p,
                    () => new ArrayFilterFieldDescriptor<TObject>(Context, p));
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
            IFilterInputTypeDescriptor<T>.RequireStruct<TStruct>? ignore = null)
            where TStruct : struct
        {
            return ListFilter<ISingleFilter<TStruct>, IEnumerable<TStruct>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<TStruct>> List<TStruct>(
            Expression<Func<T, IEnumerable<TStruct?>>> property,
            IFilterInputTypeDescriptor<T>.RequireStruct<TStruct>? ignore = null)
            where TStruct : struct
        {
            return ListFilter<ISingleFilter<TStruct>, IEnumerable<TStruct?>>(property);
        }

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context, Type entityType) =>
            new FilterInputTypeDescriptor<T>(context, entityType);
    }
}
