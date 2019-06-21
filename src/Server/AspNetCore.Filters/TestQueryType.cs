using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace Filters
{
	public class TestQueryType : ObjectType<TestModel>
	{
		protected override void Configure(IObjectTypeDescriptor<TestModel> descriptor)
		{
			descriptor.Field("foo").Argument("test", x => x.Type<TestModelFilter>()).Type<StringType>().Resolver((context) => { 
				return "bar";
			});
		}
	}
}
