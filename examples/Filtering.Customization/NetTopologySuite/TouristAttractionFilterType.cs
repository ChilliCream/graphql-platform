using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters;

namespace Filtering.Customization
{
    public class TourstAttractionFilterType : FilterInputType<TouristAttraction>
    {
        protected override void Configure(IFilterInputTypeDescriptor<TouristAttraction> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Filter(x => x.Location).AllowDistance();
        }
    }
}