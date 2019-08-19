using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting
{
    public class SortFieldDescriptor
        : DescriptorBase<SortFieldDefintion>,
          ISortFieldDescriptor

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

            SortOperationDescriptor field =
                CreateOperation(
                    new[] { SortOperationKind.Asc, SortOperationKind.Desc });
            Filters.Add(field);
        }

        protected sealed override SortFieldDefintion Definition { get; } =
            new SortFieldDefintion();

        protected ICollection<SortOperationDescriptor> Filters { get; } =
            new List<SortOperationDescriptor>();

        protected override void OnCreateDefinition(
            SortFieldDefintion definition)
        {
            var fields = new Dictionary<NameString, SortOperationDefintion>();
            AddImplicitSorters(fields);

            Definition.Sorts.AddRange(fields.Values);
        }

        private void AddImplicitSorters(
            IDictionary<NameString, SortOperationDefintion> fields)
        {
            foreach (SortOperationDefintion filterDefinition in
                Filters.Select(t => t.CreateDefinition()))
            {
                if (!filterDefinition.Ignore)
                {
                    fields[filterDefinition.Name] = filterDefinition;
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
