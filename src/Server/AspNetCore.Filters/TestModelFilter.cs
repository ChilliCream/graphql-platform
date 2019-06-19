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
            descriptor.Filter(x => x.Test).AllowContains().And().AllowEndsWith().And().AllowStartsWith().And().AllowIn().And().AllowEquals();
            descriptor.Filter(x => x.TestInt).AllowIn().And().AllowEquals().And().AllowGreaterThan().And().AllowGreaterThanOrEquals().And().AllowLowerThan().And().AllowLowerThanOrEquals();
            descriptor.Filter<TestModelFilter>(x => x.TestModal).AllowObject();
            descriptor.Filter(x => x.TestEnum);
            // TODO: Fix the named type issues
            //  descriptor.Filter(x => x.TestEnumerableInt, x => x.AllowIn()).AllowSome(); 

            descriptor.Filter<TestModelFilter>(x => x.TestModelEnumerable).AllowSome();

        }
	}
}
