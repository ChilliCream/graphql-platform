using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortFieldDescriptor
        : DescriptorBase<SortFieldDefinition>
        , ISortFieldDescriptor
    {
        public SortFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            Definition.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            Definition.Name = context.Naming.GetMemberName(
                property, MemberKind.InputObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                property, MemberKind.InputObjectField);
            Definition.Type = context.Inspector.GetInputReturnType(property);
        }

        public ISortFieldDescriptor Ignore()
        {
            Definition.Ignore = true;
            return this;
        }

        public ISortFieldDescriptor Name(NameString value)
        {
            Definition.Name = value;
            return this;
        }

        protected sealed override SortFieldDefinition Definition { get; } =
            new SortFieldDefinition();

        protected ICollection<SortOperationDescriptor> SortOperations { get; } =
            new List<SortOperationDescriptor>();

        protected override void OnCreateDefinition(
            SortFieldDefinition definition)
        {
            SortOperationDescriptor field =
                CreateOperation(
                    new[] { SortOperationKind.Asc, SortOperationKind.Desc });
            SortOperations.Add(field);

            var fields = new Dictionary<NameString, SortOperationDefintion>();
            AddImplicitSorters(fields);

            Definition.SortableFields.AddRange(fields.Values);
        }

        private void AddImplicitSorters(
            IDictionary<NameString, SortOperationDefintion> fields)
        {
            foreach (SortOperationDefintion sortDefinition in
                SortOperations.Select(t => t.CreateDefinition()))
            {
                if (!sortDefinition.Ignore)
                {
                    fields[sortDefinition.Name] = sortDefinition;
                }
            }
        }

        private SortOperationDescriptor CreateOperation(
            IEnumerable<SortOperationKind> allowedSorts)
        {
            var operation = new SortOperation(
                allowedSorts,
                Definition.Property);

            var typeReference = new ClrTypeReference(
                typeof(SortOperationKindType),
                TypeContext.Input);

            return SortOperationDescriptor.New(
                Context,
                Definition.Name,
                typeReference,
                operation);
        }
    }
}
