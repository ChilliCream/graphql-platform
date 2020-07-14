using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionDescriptor : IFilterConventionDescriptor
    {
        protected ICollection<FilterOperationConventionDescriptor> Operations { get; set; } =
            new List<FilterOperationConventionDescriptor>();

        protected FilterConventionDescriptor(IConventionContext context)
        {
            Definition.Scope = context.Scope;
        }

        protected FilterConventionDefinition Definition { get; set; }
            = new FilterConventionDefinition();

        public IFilterOperationConventionDescriptor Operation(int operation)
        {
            FilterOperationConventionDescriptor? descriptor = Operations.FirstOrDefault(
                x => x.Definition.Operation == operation);

            if (descriptor is null)
            {
                descriptor = new FilterOperationConventionDescriptor(operation);
                Operations.Add(descriptor);
            }
            return descriptor;
        }

        public FilterConventionDefinition CreateDefinition()
        {
            Definition.Operations = Operations.Select(x => x.CreateDefinition());
            return Definition;
        }

        public static FilterConventionDescriptor New(IConventionContext context) =>
            new FilterConventionDescriptor(context);
    }
}
