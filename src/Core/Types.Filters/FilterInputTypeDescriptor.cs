using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;
using HotChocolate.Types.Descriptors;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeDescriptor<T>
        : DescriptorBase<InputObjectTypeDefinition>
        , IFilterInputTypeDescriptor<T>
    {
        public FilterInputTypeDescriptor(
            IDescriptorContext context,
            Type clrType)
            : base(context)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(
                clrType, TypeKind.InputObject);
            Definition.Description = context.Naming.GetTypeDescription(
                clrType, TypeKind.InputObject);
        }

        protected override InputObjectTypeDefinition Definition { get; } =
            new InputObjectTypeDefinition();

        protected List<FilterFieldDescriptorBase> Fields { get; } =
            new List<FilterFieldDescriptorBase>();

        protected override void OnCreateDefinition(
            InputObjectTypeDefinition definition)
        {
            var fields = new Dictionary<NameString, FilterOperationDefintion>();
            var handledProperties = new HashSet<PropertyInfo>();

            FieldDescriptorUtilities.AddExplicitFields(
                Fields.Select(t => t.CreateDefinition())
                        .SelectMany(t => t.Filters),
                    f => f.Operation.Property,
                    fields,
                    handledProperties);

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<PropertyInfo> handledProperties)
        {
            if (Definition.Fields.IsImplicitBinding()
                && Definition.ClrType != typeof(object))
            {
                foreach (PropertyInfo property in Context.Inspector
                    .GetMembers(Definition.ClrType)
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

            definition = null;
            return false;
        }

        public IFilterInputTypeDescriptor<T> BindFields(
            BindingBehavior bindingBehavior)
        {
            Definition.Fields.BindingBehavior = bindingBehavior;
            return this;
        }

        public IStringFilterFieldDescriptor Filter(
            Expression<Func<T, string>> propertyOrMethod)
        {
            if (propertyOrMethod.ExtractMember() is PropertyInfo p)
            {
                var field = new StringFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(propertyOrMethod));
        }


        public IBooleanFilterFieldDescriptor Filter(
            Expression<Func<T, bool>> propertyOrMethod)
        {
            if (propertyOrMethod.ExtractMember() is PropertyInfo p)
            {
                var field = new BooleanFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(propertyOrMethod));
        }


        public IComparableFilterFieldDescriptor Filter<TComparable>(
            Expression<Func<T, TComparable>> propertyOrMethod) where TComparable : IComparable
        {
            if (propertyOrMethod.ExtractMember() is PropertyInfo p)
            {
                var field = new ComparableFilterFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(propertyOrMethod));
        }

        public static FilterInputTypeDescriptor<T> New(
            IDescriptorContext context, Type clrType) =>
            new FilterInputTypeDescriptor<T>(context, clrType);
    }
}
