using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterFieldDescriptorBase
        : DescriptorBase<FilterFieldDefintion>
    {
        protected FilterFieldDescriptorBase(
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

        protected override FilterFieldDefintion Definition { get; } =
            new FilterFieldDefintion();

        protected ICollection<FilterOperationDescriptorBase> Filters { get; } =
            new List<FilterOperationDescriptorBase>();

        protected override void OnCreateDefinition(
            FilterFieldDefintion definition)
        {
            var fields = new Dictionary<NameString, FilterOperationDefintion>();
            var handledOperations = new HashSet<NameString>();

            AddExplicitFilters(fields, handledOperations);
            OnCompleteFilters(fields, handledOperations);

            Definition.Filters.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFilters(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<NameString> handledFilterKinds)
        {
        }

        private void AddExplicitFilters(
            IDictionary<NameString, FilterOperationDefintion> fields,
            ISet<NameString> handledFilterKinds)
        {
            foreach (FilterOperationDefintion filterDefinition in
                Filters.Select(t => t.CreateDefinition()))
            {
                if (!filterDefinition.Ignore)
                {
                    fields[filterDefinition.Name] = filterDefinition;
                }

                handledFilterKinds.Add(filterDefinition.Operation);
            }
        }

        protected FilterFieldDescriptorBase BindFilters(
            BindingBehavior bindingBehavior)
        {
            Definition.Filters.BindingBehavior = bindingBehavior;
            return this;
        }
    }
}
