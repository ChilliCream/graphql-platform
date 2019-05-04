using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types.Filters;

namespace Filters
{
	public class TestModelFilter : FilterInputType<TestModel>
	{
		protected override void Configure(IFilterInputObjectTypeDescriptor<TestModel> descriptor)
		{
			Name = "TestModelFilter";
            descriptor.Filter(x => x.Test).AllowContains();


		}
	}
}
