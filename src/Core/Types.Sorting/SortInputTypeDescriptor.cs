using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting
{
    public class SortInputTypeDescriptor<T>
        : DescriptorBase<SortInputTypeDefinition>
        , ISortInputTypeDescriptor<T>
    {
        protected SortInputTypeDescriptor(
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
            Definition.Name = context.Naming.GetTypeName(
                entityType, TypeKind.Object) + "Sort";
            Definition.Description = context.Naming.GetTypeDescription(
                entityType, TypeKind.Object);
        }

        protected sealed override SortInputTypeDefinition Definition { get; } =
            new SortInputTypeDefinition();

        protected ICollection<SortFieldDescriptor> Fields { get; } =
            new List<SortFieldDescriptor>();

        public ISortInputTypeDescriptor<T> BindFields(
            BindingBehavior behavior)
        {
            Definition.Fields.BindingBehavior = behavior;
            return this;
        }

        public ISortInputTypeDescriptor<T> BindFieldsImplicitly() =>
            BindFields(BindingBehavior.Implicit);

        public ISortInputTypeDescriptor<T> BindFieldsExplicitly() =>
            BindFields(BindingBehavior.Explicit);

        public ISortFieldDescriptor Sortable(
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = new SortFieldDescriptor(Context, p);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        protected override void OnCreateDefinition(
            SortInputTypeDefinition definition)
        {
            var fields = new Dictionary<NameString, SortOperationDefintion>();
            var handledProperties = new HashSet<PropertyInfo>();

            List<SortFieldDefinition> explicitFields =
                Fields.Select(t => t.CreateDefinition()).ToList();

            FieldDescriptorUtilities.AddExplicitFields(
                explicitFields.Where(t => !t.Ignore).SelectMany(t => t.SortableFields),
                    f => f.Operation.Property,
                    fields,
                    handledProperties);

            foreach (SortFieldDefinition field in explicitFields.Where(t => t.Ignore))
            {
                handledProperties.Add(field.Property);
            }

            OnCompleteFields(fields, handledProperties);

            Definition.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<NameString, SortOperationDefintion> fields,
            ISet<PropertyInfo> handledProperties)
        {
            if (!Definition.Fields.IsImplicitBinding() ||
                Definition.EntityType == typeof(object))
            {
                return;
            }

            foreach (PropertyInfo property in Context.Inspector
                .GetMembers(Definition.EntityType)
                .OfType<PropertyInfo>())
            {
                if (handledProperties.Contains(property)
                    || !TryCreateImplicitSorting(property, out SortFieldDefinition definition))
                {
                    continue;
                }

                foreach (SortOperationDefintion sortOperation in
                    definition.SortableFields)
                {
                    if (!fields.ContainsKey(sortOperation.Name))
                    {
                        fields[sortOperation.Name] = sortOperation;
                    }
                }
            }
        }

        private bool TryCreateImplicitSorting(
            PropertyInfo property,
            out SortFieldDefinition definition)
        {
            if (typeof(IComparable).IsAssignableFrom(property.PropertyType))
            {
                var field = new SortFieldDescriptor(Context, property);
                definition = field.CreateDefinition();
                return true;
            }

            definition = null;
            return false;
        }

        public static SortInputTypeDescriptor<T> New(
            IDescriptorContext context, Type entityType) =>
            new SortInputTypeDescriptor<T>(context, entityType);
    }
}
