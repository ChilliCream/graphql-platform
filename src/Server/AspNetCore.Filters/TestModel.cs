using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Filters
{
	public class TestModel
	{
		public string Test { get; set; }
		public int TestInt { get; set; }
		public TestModel TestModal { get; set; }
		public IEnumerable<string> TestEnumerable { get; set; }
		public IEnumerable<TestModel> TestModelEnumerable { get; set; }
	}
}
