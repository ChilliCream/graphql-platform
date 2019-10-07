using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Types.Filters.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections;

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

        protected override FilterInputTypeDefinition Definition { get; } =
            new FilterInputTypeDefinition();

        protected List<FilterFieldDescriptorBase> Fields { get; } =
            new List<FilterFieldDescriptorBase>();

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

            if (typeof(IComparable).IsAssignableFrom(property.PropertyType))
            {
                var field = new ComparableFilterFieldDescriptor(
                    Context, property);
                definition = field.CreateDefinition();
                return true;
            }

            if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                ArrayFilterFieldDescriptor field;

                var genericTypeArgument = property.PropertyType.GetGenericArguments()[0];

                if (genericTypeArgument.IsGenericType && Nullable.GetUnderlyingType(genericTypeArgument) is Type nullableType)
                {
                    genericTypeArgument = nullableType;
                }
                if (genericTypeArgument == typeof(string)
                    || genericTypeArgument == typeof(bool)
                    || genericTypeArgument == typeof(bool?)
                    || typeof(IComparable).IsAssignableFrom(genericTypeArgument))
                {
                    field = new ArrayFilterFieldDescriptor(
                        Context,
                        property,
                        typeof(ISingleFilter<>).MakeGenericType(genericTypeArgument)
                        );

                }
                else
                {
                    field = new ArrayFilterFieldDescriptor(Context, property, genericTypeArgument);

                }
                definition = field.CreateDefinition();
                return true;
            }


            if (property.PropertyType.IsClass)
            {
                var field = new ObjectFilterFieldDescriptor(
                    Context, property, property.PropertyType);
                definition = field.CreateDefinition();
                return true;
            }

            definition = null;
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

        public IObjectFilterFieldDescriptor<TObject> Filter<TObject>(
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

        public IArrayFilterFieldDescriptor<TObject> Filter<TObject>(
            Expression<Func<T, IEnumerable<TObject>>> property)
            where TObject : class
        {
            return ListFilter<TObject, IEnumerable<TObject>>(property);
        }

        public IArrayFilterFieldDescriptor<TObject> Filter<TObject>(
            Expression<Func<T, List<TObject>>> property)
            where TObject : class
        {
            return ListFilter<TObject, List<TObject>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<string>> Filter(Expression<Func<T, IEnumerable<string>>> property)
        {
            return ListFilter<ISingleFilter<string>, IEnumerable<string>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<string>> Filter(Expression<Func<T, List<string>>> property)
        {
            return ListFilter<ISingleFilter<string>, List<string>>(property);
        }
        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context, Type entityType) =>
            new FilterInputTypeDescriptor<T>(context, entityType);

        public IArrayFilterFieldDescriptor<ISingleFilter<bool>> Filter(Expression<Func<T, IEnumerable<bool>>> property)
        {
            return ListFilter<ISingleFilter<bool>, IEnumerable<bool>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<bool>> Filter(Expression<Func<T, List<bool>>> property)
        {
            return ListFilter<ISingleFilter<bool>, List<bool>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<IComparable>> Filter(Expression<Func<T, IEnumerable<IComparable>>> property)
        {
            return ListFilter<ISingleFilter<IComparable>, IEnumerable<IComparable>>(property);
        }

        public IArrayFilterFieldDescriptor<ISingleFilter<IComparable>> Filter(Expression<Func<T, List<IComparable>>> property)
        {
            return ListFilter<ISingleFilter<IComparable>, List<IComparable>>(property);
        }
    }
}
