using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
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

        protected ICollection<SortOperationDescriptor> Fields { get; } =
            new List<SortOperationDescriptor>();


        public ISortInputTypeDescriptor<T> Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public ISortInputTypeDescriptor<T> Description(
            string value)
        {
            Definition.Description = value;
            return this;
        }

        public ISortInputTypeDescriptor<T> Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public ISortInputTypeDescriptor<T> Directive<TDirective>()
            where TDirective : class, new()
        {
            Definition.AddDirective(new TDirective());
            return this;
        }

        public ISortInputTypeDescriptor<T> Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }


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

        public ISortOperationDescriptor Sortable(
            Expression<Func<T, IComparable>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = SortOperationDescriptor.CreateOperation(p, Context);
                Fields.Add(field);
                return field;
            }

            // TODO : resources
            throw new ArgumentException(
                "Only properties are allowed for input types.",
                nameof(property));
        }

        public ISortObjectOperationDescriptor<TObject> SortableObject<TObject>(
            Expression<Func<T, TObject>> property)
            where TObject : class
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                var field = SortObjectOperationDescriptor<TObject>.CreateOperation(p, Context);
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

            List<SortOperationDefintion> explicitFields =
                Fields.Select(x => x.CreateDefinition()).ToList();

            FieldDescriptorUtilities.AddExplicitFields(
                explicitFields.Where(t => !t.Ignore),
                    f => f.Operation.Property,
                    fields,
                    handledProperties);

            foreach (SortOperationDefintion field in explicitFields.Where(t => t.Ignore))
            {
                handledProperties.Add(field.Operation.Property);
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
                if (handledProperties.Contains(property))
                {
                    continue;
                }

                if (TryCreateImplicitSorting(property, out SortOperationDefintion definition)
                    && !fields.ContainsKey(definition.Name))
                {
                    fields[definition.Name] = definition;
                }
            }
        }

        private bool TryCreateImplicitSorting(
            PropertyInfo property,
            out SortOperationDefintion definition)
        {
            if (typeof(IComparable).IsAssignableFrom(property.PropertyType))
            {
                definition = SortOperationDescriptor
                    .CreateOperation(property, Context)
                    .CreateDefinition();
                return true;
            }
            if (
                property.PropertyType.IsClass &&
                !typeof(IEnumerable<>).IsAssignableFrom(property.PropertyType))
            {
                definition = SortObjectOperationDescriptor
                    .CreateOperation(property, Context)
                    .CreateDefinition();
                return true;
            }

            definition = null;
            return false;
        }

        public static SortInputTypeDescriptor<T> New(
            IDescriptorContext context,
            Type entityType) =>
            new SortInputTypeDescriptor<T>(context, entityType);
    }
}
